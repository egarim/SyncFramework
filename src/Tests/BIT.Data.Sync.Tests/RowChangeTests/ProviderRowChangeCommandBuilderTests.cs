using BIT.Data.Sync.EfCore;
using BIT.Data.Sync.EfCore.MySql;
using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.SqlServer;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace BIT.Data.Sync.Tests.RowChangeTests
{
    /// <summary>
    /// Rendering neutral row changes as SQL Server, PostgreSQL and MySQL commands (T-9). Only the command text and
    /// parameters are checked; nothing is executed, so no database server is needed.
    /// </summary>
    public class ProviderRowChangeCommandBuilderTests
    {
        /// <summary>One provider under test: how it builds a command, and how it names its schema, booleans and Guids.</summary>
        public sealed class Provider
        {
            public string Name { get; set; }
            public Func<RowChangeColumnTypes, Func<RowChange, DbCommand>> Builder { get; set; }
            public string Table { get; set; }
            public string QualifiedBlogs { get; set; }
            public string BoolStoreType { get; set; }
            public string GuidStoreType { get; set; }
            public Func<DbParameter, string> ProviderType { get; set; }
            public Func<string, string> Quote { get; set; }

            public DbCommand Build(RowChange rowChange, RowChangeColumnTypes columnTypes = null) => Builder(columnTypes)(rowChange);

            public override string ToString() => Name;
        }

        public static readonly Provider SqlServer = new Provider
        {
            Name = "SqlServer",
            Builder = types => new SqlServerRowChangeCommandBuilder(types).CreateCommand,
            Table = "dbo.Blogs",
            QualifiedBlogs = "[dbo].[Blogs]",
            BoolStoreType = "bit",
            GuidStoreType = "uniqueidentifier",
            ProviderType = p => ((SqlParameter)p).SqlDbType.ToString(),
            Quote = n => "[" + n + "]",
        };

        public static readonly Provider Npgsql = new Provider
        {
            Name = "Npgsql",
            Builder = types => new NpgsqlRowChangeCommandBuilder(types).CreateCommand,
            Table = "public.Blogs",
            QualifiedBlogs = "\"public\".\"Blogs\"",
            BoolStoreType = "boolean",
            GuidStoreType = "uuid",
            ProviderType = p => ((global::Npgsql.NpgsqlParameter)p).NpgsqlDbType.ToString(),
            Quote = n => "\"" + n + "\"",
        };

        public static readonly Provider MySql = new Provider
        {
            Name = "MySql",
            Builder = types => new MySqlRowChangeCommandBuilder(types).CreateCommand,
            Table = "Blogs",
            QualifiedBlogs = "`Blogs`",
            BoolStoreType = "tinyint(1)",
            GuidStoreType = "char(36)",
            ProviderType = p => ((MySqlParameter)p).MySqlDbType.ToString(),
            Quote = n => "`" + n + "`",
        };

        public static readonly Provider[] Providers = { SqlServer, Npgsql, MySql };

        private static string Normalize(string text) => Regex.Replace(text, @"\s+", " ").Trim();

        private static List<DbParameter> Parameters(DbCommand command) => command.Parameters.Cast<DbParameter>().ToList();

        private static RowChange Insert(string table) => new RowChange
        {
            Table = table,
            Operation = RowChangeOperation.Insert,
            Keys = { new RowColumn("Id", 1) },
            Columns = { new RowColumn("Name", "A") },
        };

        private static RowChange Update(string table) => new RowChange
        {
            Table = table,
            Operation = RowChangeOperation.Update,
            Keys = { new RowColumn("Id", 1) },
            Columns = { new RowColumn("Name", "B") },
        };

        private static RowChange Delete(string table) => new RowChange
        {
            Table = table,
            Operation = RowChangeOperation.Delete,
            Keys = { new RowColumn("Id", 1) },
        };

        private static void AssertParameters(DbCommand command, params object[] values)
        {
            var parameters = Parameters(command);
            CollectionAssert.AreEqual(values.Select((_, i) => "@p" + i), parameters.Select(p => p.ParameterName));
            CollectionAssert.AreEqual(values, parameters.Select(p => p.Value));
            for (int i = 0; i < values.Length; i++)
                Assert.AreEqual(values[i].GetType(), parameters[i].Value.GetType(), "@p" + i);
        }

        // --- Insert, update and delete text ---

        [TestCase("SqlServer", "INSERT INTO [dbo].[Blogs] ([Id], [Name]) VALUES (@p0, @p1)")]
        [TestCase("Npgsql", "INSERT INTO \"public\".\"Blogs\" (\"Id\", \"Name\") VALUES (@p0, @p1)")]
        [TestCase("MySql", "INSERT INTO `Blogs` (`Id`, `Name`) VALUES (@p0, @p1)")]
        public void InsertIsRenderedWithQuotedIdentifiersAndAParameterPerValue(string providerName, string expected)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var command = provider.Build(Insert(provider.Table));
            Assert.AreEqual(expected, Normalize(command.CommandText));
            AssertParameters(command, 1, "A");
        }

        [TestCaseSource(nameof(Providers))]
        public void ATableWithoutASchemaGetsTheProvidersDefaultSchema(Provider provider)
        {
            var command = provider.Build(Insert("Blogs"));
            StringAssert.StartsWith("INSERT INTO " + provider.QualifiedBlogs + " (", Normalize(command.CommandText));
        }

        [Test]
        public void ASchemaInTheTableNameIsKeptAndADefaultSchemaCanBeChosen()
        {
            Assert.AreEqual("[sales].[Blogs]", new SqlServerRowChangeCommandBuilder().QuoteTable("sales.Blogs"));
            Assert.AreEqual("[sales].[Blogs]", new SqlServerRowChangeCommandBuilder(defaultSchema: "sales").QuoteTable("Blogs"));
            Assert.AreEqual("\"sales\".\"Blogs\"", new NpgsqlRowChangeCommandBuilder().QuoteTable("sales.Blogs"));
            Assert.AreEqual("`shop`.`Blogs`", new MySqlRowChangeCommandBuilder().QuoteTable("shop.Blogs"));
            Assert.AreEqual("`shop`.`Blogs`", new MySqlRowChangeCommandBuilder(defaultSchema: "shop").QuoteTable("Blogs"));
        }

        [TestCase("SqlServer", "UPDATE [dbo].[Blogs] SET [Name] = @p0 WHERE [Id] = @p1")]
        [TestCase("Npgsql", "UPDATE \"public\".\"Blogs\" SET \"Name\" = @p0 WHERE \"Id\" = @p1")]
        [TestCase("MySql", "UPDATE `Blogs` SET `Name` = @p0 WHERE `Id` = @p1")]
        public void UpdateSetsOnlyTheColumnsAndMatchesOnlyTheKey(string providerName, string expected)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var command = provider.Build(Update(provider.Table));
            Assert.AreEqual(expected, Normalize(command.CommandText));
            AssertParameters(command, "B", 1);
        }

        [TestCase("SqlServer", "DELETE FROM [dbo].[Blogs] WHERE [Id] = @p0")]
        [TestCase("Npgsql", "DELETE FROM \"public\".\"Blogs\" WHERE \"Id\" = @p0")]
        [TestCase("MySql", "DELETE FROM `Blogs` WHERE `Id` = @p0")]
        public void DeleteMatchesOnlyTheKey(string providerName, string expected)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var command = provider.Build(Delete(provider.Table));
            Assert.AreEqual(expected, Normalize(command.CommandText));
            AssertParameters(command, 1);
        }

        [TestCaseSource(nameof(Providers))]
        public void ACompositeKeyIsMatchedOnEveryKeyColumn(Provider provider)
        {
            var rowChange = new RowChange
            {
                Table = "Blogs",
                Operation = RowChangeOperation.Delete,
                Keys = { new RowColumn("TenantId", 7), new RowColumn("Id", 1) },
            };
            var command = provider.Build(rowChange);
            Assert.AreEqual($"DELETE FROM {provider.QualifiedBlogs} WHERE {provider.Quote("TenantId")} = @p0 AND {provider.Quote("Id")} = @p1", Normalize(command.CommandText));
            AssertParameters(command, 7, 1);
        }

        // --- Escaping and no inlined values ---

        [TestCase("SqlServer", "a]b", "[a]]b]")]
        [TestCase("Npgsql", "a\"b", "\"a\"\"b\"")]
        [TestCase("MySql", "a`b", "`a``b`")]
        public void AnIdentifierWithTheQuoteCharacterIsEscaped(string providerName, string column, string quoted)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var rowChange = new RowChange
            {
                Table = "Blogs",
                Operation = RowChangeOperation.Update,
                Keys = { new RowColumn(column, 1) },
                Columns = { new RowColumn(column + "x", "v") },
            };
            var text = Normalize(provider.Build(rowChange).CommandText);
            Assert.AreEqual($"UPDATE {provider.QualifiedBlogs} SET {quoted.Substring(0, quoted.Length - 1)}x{quoted[quoted.Length - 1]} = @p0 WHERE {quoted} = @p1", text);
        }

        [Test]
        public void QuotedTableAndSchemaNamesAreEscapedToo()
        {
            Assert.AreEqual("[d]]bo].[Bl]]ogs]", new SqlServerRowChangeCommandBuilder().QuoteTable("d]bo.Bl]ogs"));
            Assert.AreEqual("\"pub\"\"lic\".\"Bl\"\"ogs\"", new NpgsqlRowChangeCommandBuilder().QuoteTable("pub\"lic.Bl\"ogs"));
            Assert.AreEqual("`Bl``ogs`", new MySqlRowChangeCommandBuilder().QuoteTable("Bl`ogs"));
        }

        [TestCaseSource(nameof(Providers))]
        public void AStringValueWithAQuoteOrSemicolonIsOnlyAParameterValue(Provider provider)
        {
            const string hostile = "x'); DROP TABLE Blogs; --";
            foreach (var rowChange in new[] { Insert(provider.Table), Update(provider.Table), Delete(provider.Table) })
            {
                foreach (var column in rowChange.Keys.Concat(rowChange.Columns))
                    column.Value = hostile;
                var command = provider.Build(rowChange);
                StringAssert.DoesNotContain("'", command.CommandText);
                StringAssert.DoesNotContain(";", command.CommandText);
                StringAssert.DoesNotContain("DROP", command.CommandText);
                Assert.That(Parameters(command).Select(p => p.Value), Is.All.EqualTo(hostile));
                Assert.AreEqual(rowChange.Keys.Count + rowChange.Columns.Count, command.Parameters.Count);
            }
        }

        [TestCaseSource(nameof(Providers))]
        public void NoValueIsEverInlinedInTheCommandText(Provider provider)
        {
            var rowChange = new RowChange
            {
                Table = provider.Table,
                Operation = RowChangeOperation.Insert,
                Keys = { new RowColumn("Id", 424242) },
                Columns = { new RowColumn("Name", "Zebra"), new RowColumn("Price", 12.75m), new RowColumn("Stamp", new DateTime(2031, 1, 2)) },
            };
            var text = provider.Build(rowChange).CommandText;
            foreach (var inlined in new[] { "424242", "Zebra", "12.75", "2031" })
                StringAssert.DoesNotContain(inlined, text);
        }

        [TestCaseSource(nameof(Providers))]
        public void InvalidRowChangesAreRefused(Provider provider)
        {
            Assert.Throws<ArgumentNullException>(() => provider.Build(null));
            var noKey = Update(provider.Table);
            noKey.Keys.Clear();
            StringAssert.Contains("no key", Assert.Throws<ArgumentException>(() => provider.Build(noKey)).Message);
            var deleteAll = Delete(provider.Table);
            deleteAll.Keys.Clear();
            StringAssert.Contains("no key", Assert.Throws<ArgumentException>(() => provider.Build(deleteAll)).Message);
            var noColumns = Update(provider.Table);
            noColumns.Columns.Clear();
            Assert.Throws<ArgumentException>(() => provider.Build(noColumns));
            var badOperation = Insert(provider.Table);
            badOperation.Operation = (RowChangeOperation)42;
            Assert.Throws<ArgumentException>(() => provider.Build(badOperation));
            var emptyName = Insert(provider.Table);
            emptyName.Columns.Add(new RowColumn("", 1));
            Assert.Throws<ArgumentException>(() => provider.Build(emptyName));
            var nul = Insert(provider.Table);
            nul.Columns.Add(new RowColumn("a\0b", 1));
            Assert.Throws<ArgumentException>(() => provider.Build(nul));
            var twice = Insert(provider.Table);
            twice.Columns.Add(new RowColumn("Name", "again"));
            Assert.Throws<ArgumentException>(() => provider.Build(twice));
            Assert.Throws<ArgumentException>(() => provider.Build(Insert(".Blogs")));
            Assert.Throws<ArgumentException>(() => provider.Build(Insert(null)));
        }

        // --- Typed values ---

        private static DbParameter Single(Provider provider, object value, string storeType = null)
        {
            var types = storeType == null ? null : new RowChangeColumnTypes().Add("Blogs", "Value", storeType);
            var rowChange = new RowChange { Table = provider.Table, Operation = RowChangeOperation.Insert, Columns = { new RowColumn("Value", value) } };
            return Parameters(provider.Build(rowChange, types)).Single();
        }

        private static void AssertParameter(DbParameter parameter, Provider provider, object value, DbType dbType, string providerType)
        {
            Assert.AreEqual(value.GetType(), parameter.Value.GetType(), "CLR type");
            Assert.AreEqual(value, parameter.Value);
            // MySql.Data reports DbType.Object for every blob type, so for MySQL the blob type is checked as its MySqlDbType.
            if (!(provider == MySql && providerType.EndsWith("Blob")))
                Assert.AreEqual(dbType, parameter.DbType, "DbType");
            Assert.AreEqual(providerType, provider.ProviderType(parameter), "provider type");
        }

        private static readonly Guid guid = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e");

        [TestCase("SqlServer", DbType.Guid, "UniqueIdentifier")]
        [TestCase("Npgsql", DbType.Guid, "Uuid")]
        [TestCase("MySql", DbType.Guid, "Guid")]
        public void AGuidIsAGuidParameter(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            AssertParameter(Single(provider, guid), provider, guid, dbType, providerType);
            AssertParameter(Single(provider, guid, provider.GuidStoreType), provider, guid, dbType, providerType);
        }

        [TestCase("SqlServer", DbType.Guid, "UniqueIdentifier")]
        [TestCase("Npgsql", DbType.Guid, "Uuid")]
        [TestCase("MySql", DbType.Guid, "Guid")]
        public void AGuidStoredAsTextIsAGuidParameterForAGuidColumn(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            AssertParameter(Single(provider, guid.ToString(), provider.GuidStoreType), provider, guid, dbType, providerType);
            AssertParameter(Single(provider, guid.ToString("B").ToUpperInvariant(), provider.GuidStoreType.ToUpperInvariant()), provider, guid, dbType, providerType);
            AssertParameter(Single(provider, guid.ToByteArray(), provider.GuidStoreType), provider, guid, dbType, providerType);
            // Without the column type a text Guid stays text.
            Assert.AreEqual(guid.ToString(), Single(provider, guid.ToString()).Value);
            Assert.Throws<ArgumentException>(() => Single(provider, "not a guid", provider.GuidStoreType));
        }

        [TestCase("SqlServer", DbType.DateTime2, "DateTime2")]
        [TestCase("Npgsql", DbType.DateTime2, "Timestamp")]
        [TestCase("MySql", DbType.DateTime, "DateTime")]
        public void ADateTimeIsADateTimeParameter(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var value = new DateTime(2026, 11, 25, 8, 30, 15, 123);
            AssertParameter(Single(provider, value), provider, value, dbType, providerType);
        }

        [Test]
        public void ADateTimeFollowsTheColumnTypeAndOnPostgreSqlItsKind()
        {
            var utc = new DateTime(2026, 11, 25, 8, 30, 15, DateTimeKind.Utc);
            AssertParameter(Single(Npgsql, utc), Npgsql, utc, DbType.DateTime, "TimestampTz");
            AssertParameter(Single(Npgsql, utc, "timestamp with time zone"), Npgsql, utc, DbType.DateTime, "TimestampTz");
            AssertParameter(Single(Npgsql, utc, "timestamp(3) without time zone"), Npgsql, utc, DbType.DateTime2, "Timestamp");
            AssertParameter(Single(SqlServer, utc, "datetime"), SqlServer, utc, DbType.DateTime, "DateTime");
            AssertParameter(Single(MySql, utc, "datetime(6)"), MySql, utc, DbType.DateTime, "DateTime");
        }

        [TestCase("SqlServer", DbType.Boolean, "Bit")]
        [TestCase("Npgsql", DbType.Boolean, "Boolean")]
        [TestCase("MySql", DbType.Boolean, "UByte")]
        public void ABoolIsABooleanParameter(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            AssertParameter(Single(provider, true), provider, true, dbType, providerType);
            AssertParameter(Single(provider, false, provider.BoolStoreType), provider, false, dbType, providerType);
        }

        [TestCase("SqlServer", "bit", DbType.Boolean, "Bit")]
        [TestCase("Npgsql", "boolean", DbType.Boolean, "Boolean")]
        [TestCase("Npgsql", "bool", DbType.Boolean, "Boolean")]
        [TestCase("MySql", "tinyint(1)", DbType.Boolean, "UByte")]
        [TestCase("MySql", "bit(1)", DbType.Boolean, "UByte")]
        [TestCase("MySql", "boolean", DbType.Boolean, "UByte")]
        public void ABoolStoredAsZeroOrOneIsABooleanParameterForABooleanColumn(string providerName, string storeType, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            AssertParameter(Single(provider, 1L, storeType), provider, true, dbType, providerType);
            AssertParameter(Single(provider, 0L, storeType), provider, false, dbType, providerType);
            AssertParameter(Single(provider, 1, storeType), provider, true, dbType, providerType);
            AssertParameter(Single(provider, (byte)0, storeType), provider, false, dbType, providerType);
            AssertParameter(Single(provider, "1", storeType), provider, true, dbType, providerType);
            // Without the column type a 1 stays a number.
            Assert.AreEqual(1L, Single(provider, 1L).Value);
            Assert.Throws<ArgumentException>(() => Single(provider, 2L, storeType));
        }

        [TestCase("SqlServer", DbType.Decimal, "Decimal")]
        [TestCase("Npgsql", DbType.Decimal, "Numeric")]
        [TestCase("MySql", DbType.Decimal, "Decimal")]
        public void ADecimalIsADecimalParameter(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            AssertParameter(Single(provider, 1234.5600m), provider, 1234.5600m, dbType, providerType);
            AssertParameter(Single(provider, -0.01m, "decimal(18,2)"), provider, -0.01m, dbType, providerType);
        }

        [TestCase("SqlServer", DbType.Binary, "VarBinary")]
        [TestCase("Npgsql", DbType.Binary, "Bytea")]
        [TestCase("MySql", DbType.Binary, "Blob")]
        public void ABytesValueIsABinaryParameter(string providerName, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var bytes = new byte[] { 0, 1, 2, 255 };
            AssertParameter(Single(provider, bytes), provider, bytes, dbType, providerType);
        }

        [TestCase("SqlServer", "varbinary(max)", DbType.Binary, "VarBinary")]
        [TestCase("Npgsql", "bytea", DbType.Binary, "Bytea")]
        [TestCase("MySql", "longblob", DbType.Binary, "LongBlob")]
        public void ANullIsDbNullAndKeepsTheColumnType(string providerName, string storeType, DbType dbType, string providerType)
        {
            var provider = Providers.Single(p => p.Name == providerName);
            var untyped = Single(provider, null);
            Assert.AreSame(DBNull.Value, untyped.Value);
            AssertParameter(Single(provider, null, storeType), provider, DBNull.Value, dbType, providerType);
        }

        [TestCaseSource(nameof(Providers))]
        public void ANullKeyAndANullUpdateAreDbNull(Provider provider)
        {
            var rowChange = new RowChange
            {
                Table = provider.Table,
                Operation = RowChangeOperation.Update,
                Keys = { new RowColumn("Id", null) },
                Columns = { new RowColumn("Name", null) },
            };
            Assert.That(Parameters(provider.Build(rowChange)).Select(p => p.Value), Is.All.SameAs(DBNull.Value));
        }

        [Test]
        public void ColumnTypesFallBackFromASchemaQualifiedTableToItsBareName()
        {
            var types = new RowChangeColumnTypes().Add("Blogs", "Active", "bit").Add("sales.Blogs", "Active", "int");
            Assert.AreEqual("bit", types.Find("Blogs", "Active"));
            Assert.AreEqual("bit", types.Find("dbo.Blogs", "Active"));
            Assert.AreEqual("int", types.Find("sales.Blogs", "Active"));
            Assert.IsNull(types.Find("Blogs", "Other"));
            Assert.IsNull(types.Find("Posts", "Active"));
            Assert.Throws<ArgumentException>(() => types.Add("Blogs", "Active", " "));
        }

        // --- End to end: captured on SQLite, rendered for the three providers ---

        [TestCaseSource(nameof(Providers))]
        public void ADeltaCapturedOnSqliteRendersAsAnInsertOnEveryProvider(Provider provider)
        {
            using (var client = SqliteMemoryClient.Create())
            {
                client.Context.EnableRowChangeCapture = true;
                var blog = new Blog { Name = "Saved on SQLite; it's mine" };
                client.Context.Add(blog);
                client.Context.SaveChanges();

                var delta = client.DeltaStore.GetDeltasAsync(string.Empty, default).Result.Last();
                var rowChange = DeltaRowChanges.GetRowChanges(delta).Single();
                Assert.AreEqual("Blogs", rowChange.Table);

                var command = provider.Build(rowChange);
                var text = Normalize(command.CommandText);
                Assert.AreEqual($"INSERT INTO {provider.QualifiedBlogs} ({provider.Quote("Id")}, {provider.Quote("Name")}) VALUES (@p0, @p1)", text);
                AssertParameters(command, blog.Id, blog.Name);
                StringAssert.DoesNotContain(blog.Name, text);
            }
        }
    }
}
