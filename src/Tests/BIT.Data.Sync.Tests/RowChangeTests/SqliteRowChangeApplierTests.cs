using BIT.Data.Sync.EfCore;
using BIT.Data.Sync.EfCore.SQLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BIT.Data.Sync.Tests.RowChangeTests
{
    /// <summary>
    /// Applying neutral row changes to SQLite (T-8), on SQLite in-memory databases only.
    /// </summary>
    public class SqliteRowChangeApplierTests
    {
        /// <summary>Records every command it executes, with its text and parameter values.</summary>
        private class RecordingApplier : SqliteRowChangeApplier
        {
            public RecordingApplier(SqliteConnection connection) : base(connection)
            {
            }

            public List<(string Text, List<object> Values)> Executed { get; } = new List<(string, List<object>)>();

            protected override int ExecuteCommand(SqliteCommand command)
            {
                Executed.Add((command.CommandText, command.Parameters.Cast<SqliteParameter>().Select(p => p.Value).ToList()));
                return base.ExecuteCommand(command);
            }
        }

        private SqliteMemoryClient client;
        private SqliteConnection target;

        [SetUp]
        public void SetUp()
        {
            client = SqliteMemoryClient.Create();
            client.Context.EnableRowChangeCapture = true;
            target = SqliteMemoryClient.CreateTarget();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
            target.Dispose();
        }

        private RowChange CapturedRowChange() => DeltaRowChanges.GetRowChanges(client.DeltaStore.GetDeltasAsync(string.Empty, default).Result.Last()).Single();

        private static List<(long Id, string Name)> Blogs(SqliteConnection connection)
        {
            var rows = new List<(long, string)>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT \"Id\", \"Name\" FROM \"Blogs\" ORDER BY \"Id\";";
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        rows.Add((reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
            return rows;
        }

        private static long Scalar(SqliteConnection connection, string sql)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                return (long)command.ExecuteScalar();
            }
        }

        private static List<string> Schema(SqliteConnection connection)
        {
            var schema = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT type || ' ' || name || ' ' || IFNULL(sql, '') FROM sqlite_master ORDER BY name;";
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        schema.Add(reader.GetString(0));
            }
            return schema;
        }

        private static void AssertParametersOnly(RecordingApplier applier)
        {
            Assert.IsNotEmpty(applier.Executed);
            foreach (var (text, values) in applier.Executed)
            {
                Assert.IsNotEmpty(values, text);
                for (int i = 0; i < values.Count; i++)
                    StringAssert.Contains("@p" + i, text, "every value has a placeholder");
                // Without quoted identifiers and placeholders only keywords and punctuation are left: no literal of any kind.
                var rest = Regex.Replace(Regex.Replace(text, "\"(?:[^\"]|\"\")*\"", " "), "@p[0-9]+", " ");
                StringAssert.IsMatch("^[A-Z ,;()=]*$", rest, $"no row value in the command text: {text}");
                foreach (var value in values.OfType<string>().Where(v => v.Length > 3))
                    StringAssert.DoesNotContain(value, text, "no row value in the command text");
            }
        }

        [Test]
        public void CapturedInsertUpdateAndDeleteReplayOnASecondDatabase()
        {
            var blog = new Blog { Name = "a" };
            client.Context.Add(blog);
            client.Context.SaveChanges();
            var insert = CapturedRowChange();
            blog.Name = "b";
            client.Context.SaveChanges();
            var update = CapturedRowChange();
            var afterUpdate = Blogs(client.Connection);
            client.Context.Remove(blog);
            client.Context.SaveChanges();
            var delete = CapturedRowChange();

            var applier = new RecordingApplier(target);
            Assert.AreEqual(1, applier.Apply(insert));
            Assert.AreEqual(1, applier.Apply(update));
            CollectionAssert.AreEqual(afterUpdate, Blogs(target), "after insert and update the row matches the source");
            CollectionAssert.AreEqual(new[] { ((long)blog.Id, "b") }, Blogs(target));

            Assert.AreEqual(1, applier.Apply(delete));
            CollectionAssert.IsEmpty(Blogs(target), "after the delete the row is gone");
            CollectionAssert.IsEmpty(Blogs(client.Connection));

            Assert.AreEqual(3, applier.Executed.Count);
            AssertParametersOnly(applier);
        }

        [Test]
        public void ValuesAreBoundAsParametersNeverWrittenIntoTheCommand()
        {
            const string hostile = "x'); DROP TABLE Blogs; --";
            var insert = new RowChange
            {
                Table = "Blogs",
                Operation = RowChangeOperation.Insert,
                Keys = { new RowColumn("Id", 7) },
                Columns = { new RowColumn("Name", hostile) }
            };
            // Through the neutral format, as it would arrive in a delta.
            insert = RowChangeSerializer.Deserialize(RowChangeSerializer.Serialize(insert));

            var applier = new RecordingApplier(target);
            applier.Apply(insert);
            applier.Apply(new RowChange { Table = "Blogs", Operation = RowChangeOperation.Update, Keys = { new RowColumn("Id", 7) }, Columns = { new RowColumn("Name", hostile + "2") } });
            CollectionAssert.AreEqual(new[] { (7L, hostile + "2") }, Blogs(target));
            applier.Apply(new RowChange { Table = "Blogs", Operation = RowChangeOperation.Update, Keys = { new RowColumn("Id", 7) }, Columns = { new RowColumn("Name", hostile) } });

            CollectionAssert.AreEqual(new[] { (7L, hostile) }, Blogs(target), "the exact string is stored");
            Assert.AreEqual(1L, Scalar(target, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Blogs';"), "the Blogs table still exists");

            Assert.AreEqual(3, applier.Executed.Count);
            foreach (var (text, values) in applier.Executed)
            {
                StringAssert.Contains("@p0", text);
                StringAssert.DoesNotContain("DROP", text);
                StringAssert.DoesNotContain("x'", text);
                CollectionAssert.Contains(values, 7, "the key is a parameter too");
            }
            Assert.AreEqual("INSERT INTO \"Blogs\" (\"Id\", \"Name\") VALUES (@p0, @p1);", applier.Executed[0].Text);
            CollectionAssert.AreEqual(new object[] { 7, hostile }, applier.Executed[0].Values);
            AssertParametersOnly(applier);
        }

        [Test]
        public void IdentifiersAreQuotedWithEmbeddedDoubleQuotesDoubled()
        {
            Assert.AreEqual("\"Blogs\"", SqliteRowChangeApplier.QuoteIdentifier("Blogs"));
            Assert.AreEqual("\"we\"\"ird\"", SqliteRowChangeApplier.QuoteIdentifier("we\"ird"));
            Assert.AreEqual("\"a\"\"\"\"b\"", SqliteRowChangeApplier.QuoteIdentifier("a\"\"b"));
            Assert.AreEqual("\"x\"\"; DROP TABLE Blogs; --\"", SqliteRowChangeApplier.QuoteIdentifier("x\"; DROP TABLE Blogs; --"));
            Assert.Throws<ArgumentException>(() => SqliteRowChangeApplier.QuoteIdentifier(""));
            Assert.Throws<ArgumentException>(() => SqliteRowChangeApplier.QuoteIdentifier(null));

            using (var command = target.CreateCommand())
            {
                command.CommandText = "CREATE TABLE \"we\"\"ird\" (\"k\"\"ey\" INTEGER PRIMARY KEY, \"va\"\"l\" TEXT);";
                command.ExecuteNonQuery();
            }
            var applier = new RecordingApplier(target);
            applier.Apply(new RowChange { Table = "we\"ird", Operation = RowChangeOperation.Insert, Keys = { new RowColumn("k\"ey", 1L) }, Columns = { new RowColumn("va\"l", "v") } });
            applier.Apply(new RowChange { Table = "we\"ird", Operation = RowChangeOperation.Update, Keys = { new RowColumn("k\"ey", 1L) }, Columns = { new RowColumn("va\"l", "w") } });
            Assert.AreEqual(1L, Scalar(target, "SELECT COUNT(*) FROM \"we\"\"ird\" WHERE \"va\"\"l\" = 'w';"));
            applier.Apply(new RowChange { Table = "we\"ird", Operation = RowChangeOperation.Delete, Keys = { new RowColumn("k\"ey", 1L) } });
            Assert.AreEqual(0L, Scalar(target, "SELECT COUNT(*) FROM \"we\"\"ird\";"));

            Assert.AreEqual("INSERT INTO \"we\"\"ird\" (\"k\"\"ey\", \"va\"\"l\") VALUES (@p0, @p1);", applier.Executed[0].Text);
            Assert.AreEqual("UPDATE \"we\"\"ird\" SET \"va\"\"l\" = @p0 WHERE \"k\"\"ey\" = @p1;", applier.Executed[1].Text);
            Assert.AreEqual("DELETE FROM \"we\"\"ird\" WHERE \"k\"\"ey\" = @p0;", applier.Executed[2].Text);
        }

        [Test]
        public void ARowChangeForAMissingTableThrowsAndLeavesTheDatabaseUnchanged()
        {
            var applier = new SqliteRowChangeApplier(target);
            var schemaBefore = Schema(target);
            var missing = new RowChange { Table = "NoSuchTable", Operation = RowChangeOperation.Insert, Keys = { new RowColumn("Id", 1) }, Columns = { new RowColumn("Name", "a") } };

            Assert.Throws<SqliteException>(() => applier.Apply(missing));
            CollectionAssert.AreEqual(schemaBefore, Schema(target));
            CollectionAssert.IsEmpty(Blogs(target));

            // Together with a valid row change, in one call: the valid one is rolled back too.
            var valid = new RowChange { Table = "Blogs", Operation = RowChangeOperation.Insert, Keys = { new RowColumn("Id", 1) }, Columns = { new RowColumn("Name", "a") } };
            Assert.Throws<SqliteException>(() => applier.Apply(new[] { valid, missing }));
            CollectionAssert.AreEqual(schemaBefore, Schema(target));
            CollectionAssert.IsEmpty(Blogs(target), "nothing of a failed apply is kept");

            Assert.AreEqual(1, applier.Apply(valid), "the target still works after a failed apply");
        }

        [Test]
        public void UpdatesAndDeletesWithoutAKeyAreRefused()
        {
            var applier = new SqliteRowChangeApplier(target);
            applier.Apply(new RowChange { Table = "Blogs", Operation = RowChangeOperation.Insert, Keys = { new RowColumn("Id", 1) }, Columns = { new RowColumn("Name", "a") } });

            Assert.Throws<ArgumentException>(() => applier.Apply(new RowChange { Table = "Blogs", Operation = RowChangeOperation.Delete }));
            Assert.Throws<ArgumentException>(() => applier.Apply(new RowChange { Table = "Blogs", Operation = RowChangeOperation.Update, Columns = { new RowColumn("Name", "b") } }));
            Assert.Throws<ArgumentException>(() => applier.Apply(new RowChange { Table = "", Operation = RowChangeOperation.Insert, Columns = { new RowColumn("Name", "b") } }));
            CollectionAssert.AreEqual(new[] { (1L, "a") }, Blogs(target));
        }

        [Test]
        public void RowChangeTypesAndTheSettingLiveInTheCoreWithNoEfCoreOrProviderReference()
        {
            var core = typeof(RowChange).Assembly;
            Assert.AreEqual("BIT.Data.Sync", core.GetName().Name.Split(".v")[0]);
            Assert.AreSame(core, typeof(RowColumn).Assembly);
            Assert.AreSame(core, typeof(RowChangeOperation).Assembly);
            Assert.AreSame(core, typeof(IEnableRowChangeCapture).Assembly);
            foreach (var reference in core.GetReferencedAssemblies())
            {
                StringAssert.DoesNotStartWith("Microsoft.EntityFrameworkCore", reference.Name);
                StringAssert.DoesNotStartWith("Microsoft.Data", reference.Name);
                StringAssert.DoesNotStartWith("Npgsql", reference.Name);
                StringAssert.DoesNotStartWith("MySql", reference.Name);
                StringAssert.DoesNotStartWith("BIT.Data.Sync.EfCore", reference.Name);
            }

            var sqlite = typeof(SqliteRowChangeApplier).Assembly;
            StringAssert.StartsWith("BIT.Data.Sync.EfCore.Sqlite", sqlite.GetName().Name);
            foreach (var assembly in new[] { core, typeof(DeltaRowChanges).Assembly })
                Assert.IsFalse(assembly.GetTypes().Any(t => t.Name.Contains("RowChangeApplier")), $"no applier in {assembly.GetName().Name}");
        }
    }
}
