using System;
using System.Linq;

namespace BIT.Data.Sync
{
    /// <summary>
    /// A column name and its value inside a <see cref="RowChange"/>.
    /// </summary>
    /// <remarks>
    /// Any value can be assigned, but only values whose type is in <see cref="RowValueTypes.Allowed"/> can be serialized.
    /// </remarks>
    public sealed class RowColumn : IEquatable<RowColumn>
    {
        /// <summary>
        /// Initializes a new, empty instance of the <see cref="RowColumn"/> class.
        /// </summary>
        public RowColumn()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RowColumn"/> class.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <param name="value">The column value.</param>
        public RowColumn(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets or sets the column name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the column value.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Compares name, exact CLR type and value. Decimals compare with their scale, DateTime with its Kind,
        /// DateTimeOffset with its offset and byte arrays byte by byte.
        /// </summary>
        public bool Equals(RowColumn other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Name, other.Name, StringComparison.Ordinal) && ValuesEqual(Value, other.Value);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as RowColumn);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hash = Name == null ? 0 : StringComparer.Ordinal.GetHashCode(Name);
            return hash * 31 + (Value == null ? 0 : Value.GetType().GetHashCode());
        }

        /// <inheritdoc/>
        public override string ToString() => $"{Name}={Value ?? "null"}";

        internal static bool ValuesEqual(object a, object b)
        {
            if (a == null || b == null)
                return a == null && b == null;
            if (a.GetType() != b.GetType())
                return false;
            switch (a)
            {
                case byte[] bytes:
                    return bytes.SequenceEqual((byte[])b);
                case decimal d:
                    return decimal.GetBits(d).SequenceEqual(decimal.GetBits((decimal)b));
                case DateTime dt:
                    var other = (DateTime)b;
                    return dt.Ticks == other.Ticks && dt.Kind == other.Kind;
                case DateTimeOffset dto:
                    return dto.EqualsExact((DateTimeOffset)b);
                default:
                    return a.Equals(b);
            }
        }
    }
}
