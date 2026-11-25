using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BIT.Data.Sync.EfCore.SQLite
{
    /// <summary>
    /// Applies provider-neutral <see cref="RowChange"/>s to a SQLite database.
    /// </summary>
    /// <remarks>
    /// Every value is bound as a parameter; command texts hold only quoted identifiers and parameter placeholders.
    /// Table and column names are quoted as SQLite identifiers, with embedded double quotes doubled, and the
    /// table name is used as a single identifier. <see cref="Apply(IEnumerable{RowChange})"/> runs all row
    /// changes in one transaction, so a failing row change leaves the database unchanged.
    /// </remarks>
    public class SqliteRowChangeApplier
    {
        private readonly SqliteConnection connection;

        /// <param name="connection">The target database; opened on first use if it is closed.</param>
        public SqliteRowChangeApplier(SqliteConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        /// <summary>
        /// Applies one row change in its own transaction and returns the number of rows affected.
        /// </summary>
        public int Apply(RowChange rowChange) => Apply(new[] { rowChange });

        /// <summary>
        /// Applies the row changes in order, in one transaction, and returns the number of rows affected.
        /// If any of them fails, none is kept and the exception is rethrown.
        /// </summary>
        public int Apply(IEnumerable<RowChange> rowChanges)
        {
            if (rowChanges == null)
                throw new ArgumentNullException(nameof(rowChanges));
            // Check every row change first, so an invalid one is refused before anything runs.
            var rows = rowChanges.ToList();
            foreach (var rowChange in rows)
                Validate(rowChange);

            if (connection.State != ConnectionState.Open)
                connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                int affected = 0;
                foreach (var rowChange in rows)
                {
                    using (var command = CreateCommand(rowChange))
                    {
                        command.Transaction = transaction;
                        affected += ExecuteCommand(command);
                    }
                }
                transaction.Commit();
                return affected;
            }
        }

        /// <summary>
        /// Executes one command built by <see cref="CreateCommand"/>. Override to observe or log the commands.
        /// </summary>
        protected virtual int ExecuteCommand(SqliteCommand command) => command.ExecuteNonQuery();

        /// <summary>
        /// Builds the parameterized INSERT, UPDATE or DELETE for <paramref name="rowChange"/>.
        /// </summary>
        public virtual SqliteCommand CreateCommand(RowChange rowChange)
        {
            Validate(rowChange);
            var command = connection.CreateCommand();
            var text = new StringBuilder();
            var table = QuoteIdentifier(rowChange.Table);
            var keys = rowChange.Keys ?? new List<RowColumn>();
            var setColumns = rowChange.Columns ?? new List<RowColumn>();

            switch (rowChange.Operation)
            {
                case RowChangeOperation.Insert:
                    var columns = keys.Concat(setColumns).ToList();
                    text.Append("INSERT INTO ").Append(table).Append(" (")
                        .Append(string.Join(", ", columns.Select(c => QuoteIdentifier(c.Name))))
                        .Append(") VALUES (")
                        .Append(string.Join(", ", columns.Select(c => AddParameter(command, c.Value))))
                        .Append(");");
                    break;
                case RowChangeOperation.Update:
                    text.Append("UPDATE ").Append(table).Append(" SET ")
                        .Append(string.Join(", ", setColumns.Select(c => QuoteIdentifier(c.Name) + " = " + AddParameter(command, c.Value))));
                    AppendWhere(text, command, keys);
                    break;
                case RowChangeOperation.Delete:
                    text.Append("DELETE FROM ").Append(table);
                    AppendWhere(text, command, keys);
                    break;
            }
            command.CommandText = text.ToString();
            return command;
        }

        /// <summary>
        /// Quotes <paramref name="identifier"/> as a SQLite identifier: wrapped in double quotes, each embedded double quote doubled.
        /// </summary>
        public static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                throw new ArgumentException("An identifier cannot be null or empty.", nameof(identifier));
            if (identifier.IndexOf('\0') >= 0)
                throw new ArgumentException("An identifier cannot contain a NUL character.", nameof(identifier));
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        private static void AppendWhere(StringBuilder text, SqliteCommand command, List<RowColumn> keys)
        {
            text.Append(" WHERE ")
                .Append(string.Join(" AND ", keys.Select(k => QuoteIdentifier(k.Name) + " = " + AddParameter(command, k.Value))))
                .Append(';');
        }

        private static string AddParameter(SqliteCommand command, object value)
        {
            var name = "@p" + command.Parameters.Count.ToString(CultureInfo.InvariantCulture);
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return name;
        }

        private static void Validate(RowChange rowChange)
        {
            if (rowChange == null)
                throw new ArgumentNullException(nameof(rowChange));
            QuoteIdentifier(rowChange.Table);
            var keys = rowChange.Keys ?? new List<RowColumn>();
            var columns = rowChange.Columns ?? new List<RowColumn>();
            foreach (var column in keys.Concat(columns))
            {
                if (column == null)
                    throw new ArgumentException($"Row change for table '{rowChange.Table}' has a null key or column.", nameof(rowChange));
                QuoteIdentifier(column.Name);
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
