using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace BIT.Data.Sync.EfCore.Npgsql
{
    /// <summary>
    /// Builds PostgreSQL commands from provider-neutral <see cref="RowChange"/>s: identifiers in "double quotes"
    /// with " doubled, tables without a schema in "public", and a typed <see cref="NpgsqlParameter"/> per value.
    /// </summary>
    /// <remarks>
    /// A DateTime for a column of unknown type is bound as timestamp with time zone when its Kind is Utc,
    /// and as timestamp without time zone otherwise.
    /// </remarks>
    public class NpgsqlRowChangeCommandBuilder : RowChangeCommandBuilder<NpgsqlCommand>
    {
        private static readonly Dictionary<string, NpgsqlDbType> storeTypes = new Dictionary<string, NpgsqlDbType>
        {
            ["boolean"] = NpgsqlDbType.Boolean,
            ["bool"] = NpgsqlDbType.Boolean,
            ["uuid"] = NpgsqlDbType.Uuid,
            ["smallint"] = NpgsqlDbType.Smallint,
            ["int2"] = NpgsqlDbType.Smallint,
            ["integer"] = NpgsqlDbType.Integer,
            ["int"] = NpgsqlDbType.Integer,
            ["int4"] = NpgsqlDbType.Integer,
            ["bigint"] = NpgsqlDbType.Bigint,
            ["int8"] = NpgsqlDbType.Bigint,
            ["real"] = NpgsqlDbType.Real,
            ["float4"] = NpgsqlDbType.Real,
            ["double precision"] = NpgsqlDbType.Double,
            ["float8"] = NpgsqlDbType.Double,
            ["numeric"] = NpgsqlDbType.Numeric,
            ["decimal"] = NpgsqlDbType.Numeric,
            ["money"] = NpgsqlDbType.Money,
            ["date"] = NpgsqlDbType.Date,
            ["time"] = NpgsqlDbType.Time,
            ["time without time zone"] = NpgsqlDbType.Time,
            ["time with time zone"] = NpgsqlDbType.TimeTz,
            ["timetz"] = NpgsqlDbType.TimeTz,
            ["timestamp"] = NpgsqlDbType.Timestamp,
            ["timestamp without time zone"] = NpgsqlDbType.Timestamp,
            ["timestamp with time zone"] = NpgsqlDbType.TimestampTz,
            ["timestamptz"] = NpgsqlDbType.TimestampTz,
            ["interval"] = NpgsqlDbType.Interval,
            ["bytea"] = NpgsqlDbType.Bytea,
            ["text"] = NpgsqlDbType.Text,
            ["citext"] = NpgsqlDbType.Citext,
            ["character varying"] = NpgsqlDbType.Varchar,
            ["varchar"] = NpgsqlDbType.Varchar,
            ["character"] = NpgsqlDbType.Char,
            ["char"] = NpgsqlDbType.Char,
            ["json"] = NpgsqlDbType.Json,
            ["jsonb"] = NpgsqlDbType.Jsonb,
        };

        private static readonly Dictionary<DbType, NpgsqlDbType> valueTypes = new Dictionary<DbType, NpgsqlDbType>
        {
            [DbType.Boolean] = NpgsqlDbType.Boolean,
            // PostgreSQL has no one-byte integer; a byte is stored as smallint.
            [DbType.Byte] = NpgsqlDbType.Smallint,
            [DbType.Int16] = NpgsqlDbType.Smallint,
            [DbType.Int32] = NpgsqlDbType.Integer,
            [DbType.Int64] = NpgsqlDbType.Bigint,
            [DbType.Single] = NpgsqlDbType.Real,
            [DbType.Double] = NpgsqlDbType.Double,
            [DbType.Decimal] = NpgsqlDbType.Numeric,
            [DbType.Guid] = NpgsqlDbType.Uuid,
            [DbType.DateTimeOffset] = NpgsqlDbType.TimestampTz,
            [DbType.Time] = NpgsqlDbType.Interval,
            [DbType.Binary] = NpgsqlDbType.Bytea,
            [DbType.String] = NpgsqlDbType.Text,
        };

        /// <param name="columnTypes">The store types of the target columns, or null when they are unknown.</param>
        /// <param name="defaultSchema">The schema of tables named without one; "public" by default.</param>
        public NpgsqlRowChangeCommandBuilder(RowChangeColumnTypes columnTypes = null, string defaultSchema = "public")
            : base(defaultSchema, columnTypes)
        {
        }

        /// <summary>
        /// Quotes <paramref name="identifier"/> as a PostgreSQL identifier: in double quotes, each " doubled.
        /// </summary>
        public override string QuoteIdentifier(string identifier) => Quote(identifier, '"', '"');

        /// <inheritdoc/>
        protected override DbType? GetColumnDbType(string storeType)
        {
            if (!TryGetStoreType(storeType, out var npgsqlDbType))
                return null;
            switch (npgsqlDbType)
            {
                case NpgsqlDbType.Boolean: return DbType.Boolean;
                case NpgsqlDbType.Uuid: return DbType.Guid;
                default: return new NpgsqlParameter { NpgsqlDbType = npgsqlDbType }.DbType;
            }
        }

        /// <inheritdoc/>
        protected override DbParameter CreateParameter(string name, object value, DbType? dbType, string storeType)
        {
            var parameter = new NpgsqlParameter(name, value);
            if (TryGetStoreType(storeType, out var npgsqlDbType))
                parameter.NpgsqlDbType = npgsqlDbType;
            else if (value is DateTime dateTime)
                parameter.NpgsqlDbType = dateTime.Kind == DateTimeKind.Utc ? NpgsqlDbType.TimestampTz : NpgsqlDbType.Timestamp;
            else if (dbType.HasValue && valueTypes.TryGetValue(dbType.Value, out npgsqlDbType))
                parameter.NpgsqlDbType = npgsqlDbType;
            if (parameter.NpgsqlDbType == NpgsqlDbType.Smallint && value is byte b)
                parameter.Value = (short)b;
            return parameter;
        }

        private static bool TryGetStoreType(string storeType, out NpgsqlDbType npgsqlDbType)
        {
            npgsqlDbType = default;
            if (storeType == null)
                return false;
            var normalized = NormalizeStoreType(storeType);
            return storeTypes.TryGetValue(normalized, out npgsqlDbType) || storeTypes.TryGetValue(BaseStoreType(normalized), out npgsqlDbType);
        }
    }
}
