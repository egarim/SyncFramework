using System;
using System.Collections.Generic;
using System.Linq;

namespace BIT.Data.Sync
{
    /// <summary>
    /// A provider-neutral change to a single row: the table, its key columns, the operation and the column values.
    /// </summary>
    /// <remarks>
    /// Serialize it with <see cref="SerializationHelper.SerializeCore(object)"/> or <see cref="RowChangeSerializer"/>
    /// and read it back with <see cref="SerializationHelper.DeserializeCore{T}(byte[])"/> or <see cref="RowChangeSerializer"/>.
    /// </remarks>
    public sealed class RowChange : IEquatable<RowChange>
    {
        /// <summary>
        /// Gets or sets the table name, for example "dbo.Blog".
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Gets or sets the operation applied to the row.
        /// </summary>
        public RowChangeOperation Operation { get; set; }

        /// <summary>
        /// Gets or sets the key columns that identify the row. A composite key has more than one entry.
        /// </summary>
        public List<RowColumn> Keys { get; set; } = new List<RowColumn>();

        /// <summary>
        /// Gets or sets the changed column names and values.
        /// </summary>
        public List<RowColumn> Columns { get; set; } = new List<RowColumn>();

        /// <summary>
        /// Compares table, operation, keys and columns in order, field by field.
        /// </summary>
        public bool Equals(RowChange other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Table, other.Table, StringComparison.Ordinal)
                && Operation == other.Operation
                && ListEquals(Keys, other.Keys)
                && ListEquals(Columns, other.Columns);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as RowChange);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = Table == null ? 0 : StringComparer.Ordinal.GetHashCode(Table);
            return hash * 31 + (int)Operation;
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Operation} {Table}";

        private static bool ListEquals(List<RowColumn> a, List<RowColumn> b)
        {
            if (a == null || b == null)
                return a == null && b == null;
            return a.SequenceEqual(b);
        }
    }
}
