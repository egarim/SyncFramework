using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace BIT.Data.Sync.EfCore.MySql
{
    /// <summary>
    /// Builds MySQL commands from provider-neutral <see cref="RowChange"/>s: identifiers in `backticks` with `
    /// doubled, and a typed <see cref="MySqlParameter"/> per value.
    /// </summary>
    /// <remarks>
    /// A MySQL schema is a database. By default a table without a schema is left unqualified, so it is the table
    /// of the connection's database; a "schema.table" name is written as `schema`.`table`.
    /// bit(1), tinyint(1), bool and boolean columns are boolean columns, bound with <see cref="DbType.Boolean"/>;
    /// char(36) columns hold Guids.
    /// </remarks>
    public class MySqlRowChangeCommandBuilder : RowChangeCommandBuilder<MySqlCommand>
    {
        private static readonly Dictionary<string, MySqlDbType> exactStoreTypes = new Dictionary<string, MySqlDbType>
        {
            ["bit(1)"] = MySqlDbType.Bit,
            ["tinyint(1)"] = MySqlDbType.Bit,
            ["bool"] = MySqlDbType.Bit,
            ["boolean"] = MySqlDbType.Bit,
            ["char(36)"] = MySqlDbType.Guid,
        };

        private static readonly Dictionary<string, MySqlDbType> storeTypes = new Dictionary<string, MySqlDbType>
        {
            ["tinyint"] = MySqlDbType.Byte,
            ["tinyint unsigned"] = MySqlDbType.UByte,
            ["smallint"] = MySqlDbType.Int16,
            ["mediumint"] = MySqlDbType.Int24,
            ["int"] = MySqlDbType.Int32,
            ["integer"] = MySqlDbType.Int32,
            ["bigint"] = MySqlDbType.Int64,
            ["float"] = MySqlDbType.Float,
            ["double"] = MySqlDbType.Double,
            ["decimal"] = MySqlDbType.Decimal,
            ["numeric"] = MySqlDbType.Decimal,
            ["date"] = MySqlDbType.Date,
            ["time"] = MySqlDbType.Time,
            ["datetime"] = MySqlDbType.DateTime,
            ["timestamp"] = MySqlDbType.Timestamp,
            ["year"] = MySqlDbType.Year,
            ["binary"] = MySqlDbType.Binary,
            ["varbinary"] = MySqlDbType.VarBinary,
            ["tinyblob"] = MySqlDbType.TinyBlob,
            ["blob"] = MySqlDbType.Blob,
            ["mediumblob"] = MySqlDbType.MediumBlob,
            ["longblob"] = MySqlDbType.LongBlob,
            ["char"] = MySqlDbType.String,
            ["varchar"] = MySqlDbType.VarChar,
            ["tinytext"] = MySqlDbType.TinyText,
            ["text"] = MySqlDbType.Text,
            ["mediumtext"] = MySqlDbType.MediumText,
            ["longtext"] = MySqlDbType.LongText,
            ["json"] = MySqlDbType.JSON,
        };

        private static readonly Dictionary<DbType, MySqlDbType> valueTypes = new Dictionary<DbType, MySqlDbType>
        {
            [DbType.Byte] = MySqlDbType.UByte,
            [DbType.Int16] = MySqlDbType.Int16,
            [DbType.Int32] = MySqlDbType.Int32,
            [DbType.Int64] = MySqlDbType.Int64,
            [DbType.Single] = MySqlDbType.Float,
            [DbType.Double] = MySqlDbType.Double,
            [DbType.Decimal] = MySqlDbType.Decimal,
            [DbType.Guid] = MySqlDbType.Guid,
            [DbType.DateTime2] = MySqlDbType.DateTime,
            [DbType.Time] = MySqlDbType.Time,
            [DbType.Binary] = MySqlDbType.Blob,
            [DbType.String] = MySqlDbType.VarChar,
        };

        /// <param name="columnTypes">The store types of the target columns, or null when they are unknown.</param>
        /// <param name="defaultSchema">The database of tables named without one; null by default, for the connection's database.</param>
        public MySqlRowChangeCommandBuilder(RowChangeColumnTypes columnTypes = null, string defaultSchema = null)
            : base(defaultSchema, columnTypes)
        {
        }

        /// <summary>
        /// Quotes <paramref name="identifier"/> as a MySQL identifier: in backticks, each ` doubled.
        /// </summary>
        public override string QuoteIdentifier(string identifier) => Quote(identifier, '`', '`');

        /// <inheritdoc/>
        protected override DbType? GetColumnDbType(string storeType)
        {
            if (!TryGetStoreType(storeType, out var mySqlDbType))
                return null;
            switch (mySqlDbType)
            {
                case MySqlDbType.Bit: return DbType.Boolean;
                case MySqlDbType.Guid: return DbType.Guid;
                default: return new MySqlParameter { MySqlDbType = mySqlDbType }.DbType;
            }
        }

        /// <inheritdoc/>
        protected override DbParameter CreateParameter(string name, object value, DbType? dbType, string storeType)
        {
            var parameter = new MySqlParameter(name, value);
            // MySql.Data binds DbType.Boolean as tinyint unsigned, which bit(1), tinyint(1) and bool columns all accept.
            if (dbType == DbType.Boolean)
                parameter.DbType = DbType.Boolean;
            else if (TryGetStoreType(storeType, out var mySqlDbType))
                parameter.MySqlDbType = mySqlDbType;
            else if (dbType.HasValue && valueTypes.TryGetValue(dbType.Value, out mySqlDbType))
                parameter.MySqlDbType = mySqlDbType;
            // Setting the type may reset the value; bind it last.
            parameter.Value = value;
            return parameter;
        }

        private static bool TryGetStoreType(string storeType, out MySqlDbType mySqlDbType)
        {
            mySqlDbType = default;
            if (storeType == null)
                return false;
            var normalized = NormalizeStoreType(storeType);
            return exactStoreTypes.TryGetValue(normalized, out mySqlDbType)
                || storeTypes.TryGetValue(normalized, out mySqlDbType)
                || storeTypes.TryGetValue(BaseStoreType(normalized), out mySqlDbType);
        }
    }
}
