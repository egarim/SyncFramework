using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BIT.Data.Sync.EfCore
{
    /// <summary>
    /// Turns a provider-neutral <see cref="RowChange"/> into a parameterized INSERT, UPDATE or DELETE command
    /// for one database provider. The command is only built, never executed; set its connection to run it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The command text holds only quoted identifiers and parameter placeholders (@p0, @p1, ...); every value,
    /// null included, is bound as a parameter, and a null becomes <see cref="DBNull.Value"/>.
    /// </para>
    /// <para>
    /// The table may be schema-qualified as "schema.table"; it is split at the first dot. A table without a
    /// schema gets <see cref="DefaultSchema"/>, or no schema when that is null.
    /// </para>
    /// <para>
    /// An insert writes the keys, then the columns. An update sets the columns and matches the keys; a delete
    /// matches the keys. When <see cref="ColumnTypes"/> knows a column's store type, the value is converted to the
    /// column's type (a 0 or 1 to a bool for a boolean column, a text Guid to a Guid for a Guid column) and the
    /// parameter gets that type; otherwise the parameter type follows the value's CLR type.
    /// </para>
    /// </remarks>
    /// <typeparam name="TCommand">The provider's command type.</typeparam>
    public abstract class RowChangeCommandBuilder<TCommand> where TCommand : DbCommand, new()
    {
        /// <param name="defaultSchema">The schema of tables named without one, or null for none.</param>
        /// <param name="columnTypes">The store types of the target columns, or null when they are unknown.</param>
        protected RowChangeCommandBuilder(string defaultSchema, RowChangeColumnTypes columnTypes)
        {
            DefaultSchema = string.IsNullOrEmpty(defaultSchema) ? null : defaultSchema;
            ColumnTypes = columnTypes;
        }

        /// <summary>
        /// Gets the schema of tables named without one, or null for none.
        /// </summary>
        public string DefaultSchema { get; }

        /// <summary>
        /// Gets the store types of the target columns, or null when they are unknown.
        /// </summary>
        public RowChangeColumnTypes ColumnTypes { get; }

        /// <summary>
        /// Quotes <paramref name="identifier"/> as an identifier of this provider, escaping its quote character.
        /// </summary>
        public abstract string QuoteIdentifier(string identifier);

        /// <summary>
        /// Returns the <see cref="DbType"/> of a column with the given store type, or null when the store type is not known.
        /// </summary>
        protected abstract DbType? GetColumnDbType(string storeType);

        /// <summary>
        /// Creates the provider parameter <paramref name="name"/> holding <paramref name="value"/>, already converted
        /// (never null; <see cref="DBNull.Value"/> for a null).
        /// </summary>
        /// <param name="name">The parameter name, for example "@p0".</param>
        /// <param name="value">The value to bind.</param>
        /// <param name="dbType">The column type, or the type of the value when the column type is unknown; null when neither is known.</param>
        /// <param name="storeType">The store type of the column, or null when it is unknown.</param>
        protected abstract DbParameter CreateParameter(string name, object value, DbType? dbType, string storeType);

        /// <summary>
        /// Builds the parameterized INSERT, UPDATE or DELETE for <paramref name="rowChange"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The row change has an invalid name, operation or value, or no columns or key where they are needed.</exception>
        public virtual TCommand CreateCommand(RowChange rowChange)
        {
            Validate(rowChange);
            var command = new TCommand();
            var text = new StringBuilder();
            var table = QuoteTable(rowChange.Table);
            var keys = rowChange.Keys ?? new List<RowColumn>();
            var setColumns = rowChange.Columns ?? new List<RowColumn>();

            switch (rowChange.Operation)
            {
                case RowChangeOperation.Insert:
                    var columns = keys.Concat(setColumns).ToList();
                    text.Append("INSERT INTO ").Append(table).Append(" (")
                        .Append(string.Join(", ", columns.Select(c => QuoteIdentifier(c.Name))))
                        .Append(") VALUES (")
                        .Append(string.Join(", ", columns.Select(c => AddParameter(command, rowChange.Table, c))))
                        .Append(')');
                    break;
                case RowChangeOperation.Update:
                    text.Append("UPDATE ").Append(table).Append(" SET ")
                        .Append(string.Join(", ", setColumns.Select(c => QuoteIdentifier(c.Name) + " = " + AddParameter(command, rowChange.Table, c))));
                    AppendWhere(text, command, rowChange.Table, keys);
                    break;
                case RowChangeOperation.Delete:
                    text.Append("DELETE FROM ").Append(table);
                    AppendWhere(text, command, rowChange.Table, keys);
                    break;
            }
            command.CommandText = text.ToString();
            return command;
        }

        /// <summary>
        /// Quotes a table named "table" or "schema.table"; a table without a schema gets <see cref="DefaultSchema"/>.
        /// </summary>
        public virtual string QuoteTable(string table)
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentException("A table name cannot be null or empty.", nameof(table));
            string schema = DefaultSchema;
            int dot = table.IndexOf('.');
            if (dot >= 0)
            {
                schema = table.Substring(0, dot);
                table = table.Substring(dot + 1);
                if (schema.Length == 0)
                    throw new ArgumentException("A schema name cannot be empty.", nameof(table));
            }
            return schema == null ? QuoteIdentifier(table) : QuoteIdentifier(schema) + "." + QuoteIdentifier(table);
        }

        /// <summary>
        /// Wraps <paramref name="identifier"/> in <paramref name="open"/> and <paramref name="close"/>, doubling each
        /// <paramref name="close"/> inside it. Refuses an empty identifier or one with a NUL character.
        /// </summary>
        protected static string Quote(string identifier, char open, char close)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("An identifier cannot be null or empty.", nameof(identifier));
            if (identifier.IndexOf('\0') >= 0)
                throw new ArgumentException("An identifier cannot contain a NUL character.", nameof(identifier));
            return open + identifier.Replace(close.ToString(), new string(close, 2)) + close;
        }

        /// <summary>
        /// Converts a neutral value to the type of its column: null to <see cref="DBNull.Value"/>, a 0 or 1 (or "true",
        /// "false", "0", "1") to a bool for a <see cref="DbType.Boolean"/> column, and a text or 16-byte Guid to a Guid
        /// for a <see cref="DbType.Guid"/> column. Any other value is returned as it is.
        /// </summary>
        /// <exception cref="ArgumentException">The value cannot be converted to the column type.</exception>
        protected virtual object ConvertValue(object value, DbType? columnType, string table, string column)
        {
            if (value == null || value is DBNull)
                return DBNull.Value;
            switch (columnType)
            {
                case DbType.Boolean:
                    if (TryConvertToBoolean(value, out bool boolean))
                        return boolean;
                    break;
                case DbType.Guid:
                    if (value is Guid)
                        return value;
                    if (value is string text && Guid.TryParse(text, out Guid guid))
                        return guid;
                    if (value is byte[] bytes && bytes.Length == 16)
                        return new Guid(bytes);
                    break;
                default:
                    return value;
            }
            throw new ArgumentException($"Column '{column}' of '{table}' is a {columnType} column, but its value '{value}' ({value.GetType().Name}) cannot be converted to it.");
        }

        /// <summary>
        /// Returns the <see cref="DbType"/> of a value's CLR type, or null for <see cref="DBNull"/> and unknown types.
        /// </summary>
        protected static DbType? GetValueDbType(object value)
        {
            switch (value)
            {
                case bool _: return DbType.Boolean;
                case byte _: return DbType.Byte;
                case short _: return DbType.Int16;
                case int _: return DbType.Int32;
                case long _: return DbType.Int64;
                case float _: return DbType.Single;
                case double _: return DbType.Double;
                case decimal _: return DbType.Decimal;
                case Guid _: return DbType.Guid;
                case DateTime _: return DbType.DateTime2;
                case DateTimeOffset _: return DbType.DateTimeOffset;
                case TimeSpan _: return DbType.Time;
                case byte[] _: return DbType.Binary;
                case string _: return DbType.String;
                default: return null;
            }
        }

        /// <summary>
        /// Lower-cases a store type and removes extra spaces, so "Char (36)" reads as "char(36)".
        /// </summary>
        protected static string NormalizeStoreType(string storeType)
        {
            if (storeType == null)
                return null;
            var normalized = string.Join(" ", storeType.Trim().ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
            return normalized.Replace(" (", "(").Replace("( ", "(").Replace(" )", ")").Replace(", ", ",");
        }

        /// <summary>
        /// Removes the size, precision or length of a normalized store type, so "nvarchar(max)" reads as "nvarchar".
        /// </summary>
        protected static string BaseStoreType(string normalizedStoreType)
        {
            if (normalizedStoreType == null)
                return null;
            int open = normalizedStoreType.IndexOf('(');
            if (open < 0)
                return normalizedStoreType;
            int close = normalizedStoreType.IndexOf(')', open);
            var rest = close < 0 ? string.Empty : normalizedStoreType.Substring(close + 1);
            return (normalizedStoreType.Substring(0, open) + rest).Trim();
        }

        private static bool TryConvertToBoolean(object value, out bool result)
        {
            switch (value)
            {
                case bool b:
                    result = b;
                    return true;
                case string s:
                    s = s.Trim();
                    if (s == "0" || s == "1")
                    {
                        result = s == "1";
                        return true;
                    }
                    return bool.TryParse(s, out result);
                case byte _:
                case sbyte _:
                case short _:
                case ushort _:
                case int _:
                case uint _:
                case long _:
                case ulong _:
                case decimal _:
                    var number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                    result = number == 1m;
                    return number == 0m || number == 1m;
                default:
                    result = false;
                    return false;
            }
        }

        private void AppendWhere(StringBuilder text, TCommand command, string table, List<RowColumn> keys)
        {
            text.Append(" WHERE ")
                .Append(string.Join(" AND ", keys.Select(k => QuoteIdentifier(k.Name) + " = " + AddParameter(command, table, k))));
        }

        private string AddParameter(TCommand command, string table, RowColumn column)
        {
            var name = "@p" + command.Parameters.Count.ToString(CultureInfo.InvariantCulture);
            var storeType = ColumnTypes?.Find(table, column.Name);
            var columnType = storeType == null ? null : GetColumnDbType(storeType);
            var value = ConvertValue(column.Value, columnType, table, column.Name);
            command.Parameters.Add(CreateParameter(name, value, columnType ?? GetValueDbType(value), storeType));
            return name;
        }

        private void Validate(RowChange rowChange)
        {
            if (rowChange == null)
                throw new ArgumentNullException(nameof(rowChange));
            QuoteTable(rowChange.Table);
            var keys = rowChange.Keys ?? new List<RowColumn>();
            var columns = rowChange.Columns ?? new List<RowColumn>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var column in keys.Concat(columns))
            {
                if (column == null)
                    throw new ArgumentException($"Row change for table '{rowChange.Table}' has a null key or column.", nameof(rowChange));
                QuoteIdentifier(column.Name);
                if (!names.Add(column.Name) && rowChange.Operation == RowChangeOperation.Insert)
                    throw new ArgumentException($"Insert into '{rowChange.Table}' names column '{column.Name}' more than once.", nameof(rowChange));
            }
            switch (rowChange.Operation)
            {
                case RowChangeOperation.Insert:
                    if (keys.Count + columns.Count == 0)
                        throw new ArgumentException($"Insert into '{rowChange.Table}' has no columns.", nameof(rowChange));
                    break;
                case RowChangeOperation.Update:
                    if (keys.Count == 0)
                        throw new ArgumentException($"Update of '{rowChange.Table}' has no key; it would change every row.", nameof(rowChange));
                    if (columns.Count == 0)
                        throw new ArgumentException($"Update of '{rowChange.Table}' has no columns to set.", nameof(rowChange));
                    break;
                case RowChangeOperation.Delete:
                    if (keys.Count == 0)
                        throw new ArgumentException($"Delete from '{rowChange.Table}' has no key; it would delete every row.", nameof(rowChange));
                    break;
                default:
                    throw new ArgumentException($"Row change for table '{rowChange.Table}' has operation '{rowChange.Operation}', which is not Insert, Update or Delete.", nameof(rowChange));
            }
        }
    }
}
