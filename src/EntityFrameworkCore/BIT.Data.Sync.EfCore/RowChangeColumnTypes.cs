using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EfCore
{
    /// <summary>
    /// The store types of the target columns, for example "bit", "uuid" or "char(36)", used by a
    /// <see cref="RowChangeCommandBuilder{TCommand}"/> to give each parameter the column's type.
    /// </summary>
    /// <remarks>
    /// A neutral <see cref="RowChange"/> carries values only. A value captured on one database may be stored
    /// differently on another, for example a bool as 0 or 1, or a Guid as text; the store type of the target
    /// column tells the builder to turn it back into a bool or a Guid. Names compare ordinally.
    /// </remarks>
    public class RowChangeColumnTypes
    {
        private readonly Dictionary<(string Table, string Column), string> storeTypes = new Dictionary<(string Table, string Column), string>();

        /// <summary>
        /// Records the store type of <paramref name="column"/> in <paramref name="table"/> and returns this instance.
        /// </summary>
        /// <param name="table">The table as named in the row change, with or without its schema, for example "Blogs" or "dbo.Blogs".</param>
        /// <param name="column">The column name.</param>
        /// <param name="storeType">The store type of the column on the target database, for example "bit" or "char(36)".</param>
        public RowChangeColumnTypes Add(string table, string column, string storeType)
        {
            if (string.IsNullOrEmpty(table))
                throw new ArgumentException("A table name cannot be null or empty.", nameof(table));
            if (string.IsNullOrEmpty(column))
                throw new ArgumentException("A column name cannot be null or empty.", nameof(column));
            if (string.IsNullOrWhiteSpace(storeType))
                throw new ArgumentException("A store type cannot be null or empty.", nameof(storeType));
            storeTypes[(table, column)] = storeType;
            return this;
        }

        /// <summary>
        /// Returns the store type of <paramref name="column"/> in <paramref name="table"/>, or null when it is unknown.
        /// When <paramref name="table"/> is schema-qualified and has no entry, the entry of the bare table name is used.
        /// </summary>
        public string Find(string table, string column)
        {
            if (table == null || column == null)
                return null;
            if (storeTypes.TryGetValue((table, column), out var storeType))
                return storeType;
            int dot = table.IndexOf('.');
            if (dot >= 0 && storeTypes.TryGetValue((table.Substring(dot + 1), column), out storeType))
                return storeType;
            return null;
        }
    }
}
