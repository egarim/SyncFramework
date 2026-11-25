using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace BIT.Data.Sync.EfCore.SqlServer
{
    /// <summary>
    /// Builds SQL Server commands from provider-neutral <see cref="RowChange"/>s: identifiers in [brackets]
    /// with ] doubled, tables without a schema in "dbo", and a typed <see cref="SqlParameter"/> per value.
    /// </summary>
    public class SqlServerRowChangeCommandBuilder : RowChangeCommandBuilder<SqlCommand>
    {
        private static readonly Dictionary<string, SqlDbType> storeTypes = new Dictionary<string, SqlDbType>
        {
            ["bit"] = SqlDbType.Bit,
            ["uniqueidentifier"] = SqlDbType.UniqueIdentifier,
            ["tinyint"] = SqlDbType.TinyInt,
            ["smallint"] = SqlDbType.SmallInt,
            ["int"] = SqlDbType.Int,
            ["bigint"] = SqlDbType.BigInt,
            ["real"] = SqlDbType.Real,
            ["float"] = SqlDbType.Float,
            ["decimal"] = SqlDbType.Decimal,
            ["numeric"] = SqlDbType.Decimal,
            ["money"] = SqlDbType.Money,
            ["smallmoney"] = SqlDbType.SmallMoney,
            ["date"] = SqlDbType.Date,
            ["time"] = SqlDbType.Time,
            ["datetime"] = SqlDbType.DateTime,
            ["datetime2"] = SqlDbType.DateTime2,
            ["smalldatetime"] = SqlDbType.SmallDateTime,
            ["datetimeoffset"] = SqlDbType.DateTimeOffset,
            ["binary"] = SqlDbType.Binary,
            ["varbinary"] = SqlDbType.VarBinary,
            ["image"] = SqlDbType.Image,
            ["rowversion"] = SqlDbType.Timestamp,
            ["timestamp"] = SqlDbType.Timestamp,
            ["char"] = SqlDbType.Char,
            ["varchar"] = SqlDbType.VarChar,
            ["text"] = SqlDbType.Text,
            ["nchar"] = SqlDbType.NChar,
            ["nvarchar"] = SqlDbType.NVarChar,
            ["ntext"] = SqlDbType.NText,
            ["xml"] = SqlDbType.Xml,
        };

        private static readonly Dictionary<DbType, SqlDbType> valueTypes = new Dictionary<DbType, SqlDbType>
        {
            [DbType.Boolean] = SqlDbType.Bit,
            [DbType.Byte] = SqlDbType.TinyInt,
            [DbType.Int16] = SqlDbType.SmallInt,
            [DbType.Int32] = SqlDbType.Int,
            [DbType.Int64] = SqlDbType.BigInt,
            [DbType.Single] = SqlDbType.Real,
            [DbType.Double] = SqlDbType.Float,
            [DbType.Decimal] = SqlDbType.Decimal,
            [DbType.Guid] = SqlDbType.UniqueIdentifier,
            [DbType.DateTime2] = SqlDbType.DateTime2,
            [DbType.DateTimeOffset] = SqlDbType.DateTimeOffset,
            [DbType.Time] = SqlDbType.Time,
            [DbType.Binary] = SqlDbType.VarBinary,
            [DbType.String] = SqlDbType.NVarChar,
        };

        /// <param name="columnTypes">The store types of the target columns, or null when they are unknown.</param>
        /// <param name="defaultSchema">The schema of tables named without one; "dbo" by default.</param>
        public SqlServerRowChangeCommandBuilder(RowChangeColumnTypes columnTypes = null, string defaultSchema = "dbo")
            : base(defaultSchema, columnTypes)
        {
        }

        /// <summary>
        /// Quotes <paramref name="identifier"/> as a SQL Server identifier: in brackets, each ] doubled.
        /// </summary>
        public override string QuoteIdentifier(string identifier) => Quote(identifier, '[', ']');

        /// <inheritdoc/>
        protected override DbType? GetColumnDbType(string storeType)
        {
            if (!TryGetStoreType(storeType, out var sqlDbType))
                return null;
            switch (sqlDbType)
            {
                case SqlDbType.Bit: return DbType.Boolean;
                case SqlDbType.UniqueIdentifier: return DbType.Guid;
                default: return new SqlParameter { SqlDbType = sqlDbType }.DbType;
            }
        }

        /// <inheritdoc/>
        protected override DbParameter CreateParameter(string name, object value, DbType? dbType, string storeType)
        {
            var parameter = new SqlParameter(name, value);
            if (TryGetStoreType(storeType, out var sqlDbType))
                parameter.SqlDbType = sqlDbType;
            else if (dbType.HasValue && valueTypes.TryGetValue(dbType.Value, out sqlDbType))
                parameter.SqlDbType = sqlDbType;
            return parameter;
        }

        private static bool TryGetStoreType(string storeType, out SqlDbType sqlDbType)
        {
            sqlDbType = default;
            return storeType != null && storeTypes.TryGetValue(BaseStoreType(NormalizeStoreType(storeType)), out sqlDbType);
        }
    }
}
