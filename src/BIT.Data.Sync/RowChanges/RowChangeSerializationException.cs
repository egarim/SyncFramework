using System;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Thrown when a <see cref="RowChange"/> cannot be written or read back.
    /// </summary>
    /// <remarks>
    /// Writing throws it when a column holds a value whose type is not in <see cref="RowValueTypes.Allowed"/>,
    /// or when the row change itself is incomplete; no partial payload is returned.
    /// Reading throws it for any payload that is malformed, names a type tag outside the closed list,
    /// names a CLR type, or carries any member the format does not define (such as "$type" or "__type").
    /// Types named in a payload are never resolved or loaded.
    /// </remarks>
    public class RowChangeSerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RowChangeSerializationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="columnName">The column the error belongs to, or null if it belongs to the row change itself.</param>
        /// <param name="rejectedType">The offending CLR type name or payload type tag, if any.</param>
        /// <param name="innerException">The underlying exception, if any.</param>
        public RowChangeSerializationException(string message, string columnName = null, string rejectedType = null, Exception innerException = null)
            : base(message, innerException)
        {
            ColumnName = columnName;
            RejectedType = rejectedType;
        }

        /// <summary>
        /// Gets the name of the column the error belongs to, or null if it belongs to the row change itself.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the offending CLR type name (when writing) or payload type tag/member (when reading), if any.
        /// </summary>
        public string RejectedType { get; }
    }
}
