using System;
using System.Collections.Generic;

namespace BIT.Data.Sync
{
    /// <summary>
    /// The closed list of value types a <see cref="RowColumn"/> may hold in a serialized <see cref="RowChange"/>.
    /// </summary>
    /// <remarks>
    /// The list is fixed in code: null, string, bool, byte, short, int, long, float, double, decimal, Guid,
    /// DateTime, DateTimeOffset, TimeSpan and byte[]. Each type is written with its own tag and read back as
    /// exactly the same CLR type. Payloads are never allowed to name a CLR type, an assembly or a converter.
    /// </remarks>
    public static class RowValueTypes
    {
        /// <summary>The payload tag of a null value.</summary>
        public const string NullTag = "null";

        private static readonly KeyValuePair<Type, string>[] map =
        {
            new KeyValuePair<Type, string>(typeof(string), "string"),
            new KeyValuePair<Type, string>(typeof(bool), "bool"),
            new KeyValuePair<Type, string>(typeof(byte), "byte"),
            new KeyValuePair<Type, string>(typeof(short), "short"),
            new KeyValuePair<Type, string>(typeof(int), "int"),
            new KeyValuePair<Type, string>(typeof(long), "long"),
            new KeyValuePair<Type, string>(typeof(float), "float"),
            new KeyValuePair<Type, string>(typeof(double), "double"),
            new KeyValuePair<Type, string>(typeof(decimal), "decimal"),
            new KeyValuePair<Type, string>(typeof(Guid), "guid"),
            new KeyValuePair<Type, string>(typeof(DateTime), "datetime"),
            new KeyValuePair<Type, string>(typeof(DateTimeOffset), "datetimeoffset"),
            new KeyValuePair<Type, string>(typeof(TimeSpan), "timespan"),
            new KeyValuePair<Type, string>(typeof(byte[]), "bytes"),
        };

        private static readonly Dictionary<Type, string> tagByType = new Dictionary<Type, string>();
        private static readonly Dictionary<string, Type> typeByTag = new Dictionary<string, Type>(StringComparer.Ordinal);

        static RowValueTypes()
        {
            var types = new List<Type>();
            var tags = new List<string> { NullTag };
            foreach (var pair in map)
            {
                types.Add(pair.Key);
                tags.Add(pair.Value);
                tagByType.Add(pair.Key, pair.Value);
                typeByTag.Add(pair.Value, pair.Key);
            }
            Allowed = types.AsReadOnly();
            Tags = tags.AsReadOnly();
        }

        /// <summary>
        /// Gets the allowed non-null CLR value types. A null value is allowed as well.
        /// </summary>
        public static IReadOnlyList<Type> Allowed { get; }

        /// <summary>
        /// Gets every type tag a payload may contain, including <see cref="NullTag"/>.
        /// </summary>
        public static IReadOnlyList<string> Tags { get; }

        /// <summary>
        /// Returns true if <paramref name="value"/> is null or its exact type is in <see cref="Allowed"/>.
        /// </summary>
        public static bool IsAllowed(object value) => value == null || tagByType.ContainsKey(value.GetType());

        internal static bool TryGetTag(Type type, out string tag) => tagByType.TryGetValue(type, out tag);

        internal static bool TryGetType(string tag, out Type type) => typeByTag.TryGetValue(tag, out type);
    }
}
