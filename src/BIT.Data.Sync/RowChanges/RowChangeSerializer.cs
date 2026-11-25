using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Writes and strictly reads the neutral JSON format of <see cref="RowChange"/>.
    /// </summary>
    /// <remarks>
    /// A row change is written as
    /// <c>{"format":"bit.rowchange/1","table":"dbo.Blog","operation":"Update","keys":[...],"columns":[...]}</c>
    /// and every key or column as <c>{"name":"Id","type":"long","value":"42"}</c>, where <c>type</c> is one of
    /// <see cref="RowValueTypes.Tags"/> and <c>value</c> is the invariant text of the value
    /// (DateTime values also carry a <c>kind</c>). Reading accepts only these members and tags; it never resolves a
    /// type, assembly or converter named in the payload. Every failure is a <see cref="RowChangeSerializationException"/>.
    /// <see cref="SerializationHelper.SerializeCore(object)"/> and <see cref="SerializationHelper.DeserializeCore{T}(byte[])"/>
    /// use this class for <see cref="RowChange"/> and collections of it.
    /// </remarks>
    public static class RowChangeSerializer
    {
        /// <summary>The value of the "format" member of every serialized row change.</summary>
        public const string FormatId = "bit.rowchange/1";

        private const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff";
        private const string RowContext = "(row change)";

        private static readonly HashSet<string> rowMembers = new HashSet<string>(StringComparer.Ordinal) { "format", "table", "operation", "keys", "columns" };
        private static readonly HashSet<string> columnMembers = new HashSet<string>(StringComparer.Ordinal) { "name", "type", "value", "kind" };

        private static readonly JsonDocumentOptions documentOptions = new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = 16
        };

        private struct EncodedColumn
        {
            public string Name;
            public string Tag;
            public string Text;
            public string Kind;
        }

        private sealed class EncodedRow
        {
            public string Table;
            public string Operation;
            public List<EncodedColumn> Keys;
            public List<EncodedColumn> Columns;
        }

        /// <summary>
        /// Serializes one row change to UTF-8 JSON.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">The row change is incomplete or a value type is not allowed.</exception>
        public static byte[] Serialize(RowChange rowChange)
        {
            var encoded = Encode(rowChange);
            return Write(writer => WriteRow(writer, encoded));
        }

        /// <summary>
        /// Serializes a list of row changes to a UTF-8 JSON array.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">A row change is incomplete or a value type is not allowed.</exception>
        public static byte[] Serialize(IEnumerable<RowChange> rowChanges)
        {
            if (rowChanges == null)
                throw new RowChangeSerializationException("Cannot serialize a null list of row changes.");
            var encoded = rowChanges.Select(Encode).ToList();
            return Write(writer =>
            {
                writer.WriteStartArray();
                foreach (var row in encoded)
                    WriteRow(writer, row);
                writer.WriteEndArray();
            });
        }

        /// <summary>
        /// Reads one row change written by <see cref="Serialize(RowChange)"/>.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">The payload is malformed or names a type outside the closed list.</exception>
        public static RowChange Deserialize(byte[] data)
        {
            using (var document = Parse(data))
            {
                return ReadRow(document.RootElement);
            }
        }

        /// <summary>
        /// Reads a list of row changes written by <see cref="Serialize(IEnumerable{RowChange})"/>.
        /// </summary>
        /// <exception cref="RowChangeSerializationException">The payload is malformed or names a type outside the closed list.</exception>
        public static List<RowChange> DeserializeList(byte[] data)
        {
            using (var document = Parse(data))
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                    throw new RowChangeSerializationException($"A row change list payload must be a JSON array, not {root.ValueKind}.");
                return root.EnumerateArray().Select(ReadRow).ToList();
            }
        }

        #region Writing

        private static byte[] Write(Action<Utf8JsonWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    write(writer);
                }
                return stream.ToArray();
            }
        }

        private static EncodedRow Encode(RowChange rowChange)
        {
            if (rowChange == null)
                throw new RowChangeSerializationException("Cannot serialize a null row change.");
            if (string.IsNullOrEmpty(rowChange.Table))
                throw new RowChangeSerializationException("Cannot serialize a row change without a table.");
            if (!IsDefinedOperation(rowChange.Operation))
                throw new RowChangeSerializationException($"Cannot serialize row change for table '{rowChange.Table}': operation '{rowChange.Operation}' is not Insert, Update or Delete.");
            return new EncodedRow
            {
                Table = rowChange.Table,
                Operation = rowChange.Operation.ToString(),
                Keys = EncodeColumns(rowChange.Table, "key", rowChange.Keys),
                Columns = EncodeColumns(rowChange.Table, "column", rowChange.Columns)
            };
        }

        private static List<EncodedColumn> EncodeColumns(string table, string what, List<RowColumn> columns)
        {
            var result = new List<EncodedColumn>();
            if (columns == null)
                return result;
            foreach (var column in columns)
            {
                if (column == null)
                    throw new RowChangeSerializationException($"Cannot serialize row change for table '{table}': a {what} entry is null.");
                if (string.IsNullOrEmpty(column.Name))
                    throw new RowChangeSerializationException($"Cannot serialize row change for table '{table}': a {what} has no name.");
                result.Add(EncodeValue(column.Name, column.Value));
            }
            return result;
        }

        private static EncodedColumn EncodeValue(string name, object value)
        {
            var encoded = new EncodedColumn { Name = name };
            if (value == null)
            {
                encoded.Tag = RowValueTypes.NullTag;
                return encoded;
            }
            if (!RowValueTypes.TryGetTag(value.GetType(), out encoded.Tag))
                throw new RowChangeSerializationException(
                    $"Column '{name}' holds a value of type '{value.GetType().FullName}', which is not an allowed row change value type. Allowed: {AllowedList()}.",
                    name, value.GetType().FullName);

            var c = CultureInfo.InvariantCulture;
            switch (value)
            {
                case string s: encoded.Text = s; break;
                case bool b: encoded.Text = b ? "true" : "false"; break;
                case byte v: encoded.Text = v.ToString(c); break;
                case short v: encoded.Text = v.ToString(c); break;
                case int v: encoded.Text = v.ToString(c); break;
                case long v: encoded.Text = v.ToString(c); break;
                case float v: encoded.Text = v.ToString("R", c); break;
                case double v: encoded.Text = v.ToString("R", c); break;
                case decimal v: encoded.Text = v.ToString(c); break;
                case Guid v: encoded.Text = v.ToString("D"); break;
                case DateTime v:
                    encoded.Text = v.ToString(DateTimeFormat, c);
                    encoded.Kind = v.Kind.ToString();
                    break;
                case DateTimeOffset v: encoded.Text = v.ToString("O", c); break;
                case TimeSpan v: encoded.Text = v.ToString("c", c); break;
                case byte[] v: encoded.Text = Convert.ToBase64String(v); break;
            }
            return encoded;
        }

        private static void WriteRow(Utf8JsonWriter writer, EncodedRow row)
        {
            writer.WriteStartObject();
            writer.WriteString("format", FormatId);
            writer.WriteString("table", row.Table);
            writer.WriteString("operation", row.Operation);
            WriteColumns(writer, "keys", row.Keys);
            WriteColumns(writer, "columns", row.Columns);
            writer.WriteEndObject();
        }

        private static void WriteColumns(Utf8JsonWriter writer, string propertyName, List<EncodedColumn> columns)
        {
            writer.WriteStartArray(propertyName);
            foreach (var column in columns)
            {
                writer.WriteStartObject();
                writer.WriteString("name", column.Name);
                writer.WriteString("type", column.Tag);
                if (column.Text != null)
                    writer.WriteString("value", column.Text);
                if (column.Kind != null)
                    writer.WriteString("kind", column.Kind);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        #endregion

        #region Reading

        private static JsonDocument Parse(byte[] data)
        {
            if (data == null)
                throw new RowChangeSerializationException("Cannot deserialize a row change from null data.");
            try
            {
                return JsonDocument.Parse(data, documentOptions);
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                throw new RowChangeSerializationException($"Row change payload is not valid JSON: {ex.Message}", innerException: ex);
            }
        }

        private static RowChange ReadRow(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
                throw new RowChangeSerializationException($"A row change payload must be a JSON object, not {element.ValueKind}.");

            var members = ReadMembers(element, rowMembers, RowContext);

            var format = RequireString(members, "format", RowContext);
            if (!string.Equals(format, FormatId, StringComparison.Ordinal))
                throw new RowChangeSerializationException($"Row change payload has format '{format}'; only '{FormatId}' is supported.", rejectedType: format);

            var table = RequireString(members, "table", RowContext);
            if (table.Length == 0)
                throw new RowChangeSerializationException("Row change payload has an empty table.");

            var operationText = RequireString(members, "operation", RowContext);
            if (!TryParseOperation(operationText, out var operation))
                throw new RowChangeSerializationException($"Row change payload for table '{table}' has operation '{operationText}'; only Insert, Update and Delete are allowed.", rejectedType: operationText);

            return new RowChange
            {
                Table = table,
                Operation = operation,
                Keys = ReadColumns(members, "keys", table),
                Columns = ReadColumns(members, "columns", table)
            };
        }

        private static Dictionary<string, JsonElement> ReadMembers(JsonElement element, HashSet<string> allowed, string context)
        {
            var members = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                    throw new RowChangeSerializationException(
                        $"{Describe(context)}: member '{property.Name}' is not part of the row change format and was rejected; type names and converters in payloads are never resolved.",
                        context, property.Name);
                if (members.ContainsKey(property.Name))
                    throw new RowChangeSerializationException($"{Describe(context)}: member '{property.Name}' appears more than once.", context, property.Name);
                members.Add(property.Name, property.Value);
            }
            return members;
        }

        private static string RequireString(Dictionary<string, JsonElement> members, string member, string context)
        {
            if (!members.TryGetValue(member, out var value))
                throw new RowChangeSerializationException($"{Describe(context)}: required member '{member}' is missing.", context);
            if (value.ValueKind != JsonValueKind.String)
                throw new RowChangeSerializationException($"{Describe(context)}: member '{member}' must be a string, not {value.ValueKind}.", context);
            return value.GetString();
        }

        private static List<RowColumn> ReadColumns(Dictionary<string, JsonElement> members, string member, string table)
        {
            if (!members.TryGetValue(member, out var array))
                throw new RowChangeSerializationException($"Row change payload for table '{table}' has no '{member}' array.");
            if (array.ValueKind != JsonValueKind.Array)
                throw new RowChangeSerializationException($"Row change payload for table '{table}': '{member}' must be an array, not {array.ValueKind}.");

            var result = new List<RowColumn>();
            int index = 0;
            foreach (var item in array.EnumerateArray())
            {
                var position = $"{member}[{index++}]";
                if (item.ValueKind != JsonValueKind.Object)
                    throw new RowChangeSerializationException($"Column '{position}' of table '{table}' must be a JSON object, not {item.ValueKind}.", position);
                result.Add(ReadColumn(item, position));
            }
            return result;
        }

        private static RowColumn ReadColumn(JsonElement element, string position)
        {
            // Read the name first so later errors can name the column.
            string name = position;
            if (element.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(nameElement.GetString()))
                name = nameElement.GetString();

            var members = ReadMembers(element, columnMembers, name);
            if (name == position)
                throw new RowChangeSerializationException($"Column '{position}' has no name.", position);

            var tag = RequireString(members, "type", name);
            if (!RowValueTypes.TryGetType(tag, out var type) && tag != RowValueTypes.NullTag)
                throw new RowChangeSerializationException(
                    $"Column '{name}' declares the type tag '{tag}', which is not in the closed list of row change value types ({AllowedTags()}); type names and converters in payloads are never resolved.",
                    name, tag);

            members.TryGetValue("value", out var valueElement);
            bool hasValue = members.ContainsKey("value");
            bool hasKind = members.TryGetValue("kind", out var kindElement);

            if (hasKind && type != typeof(DateTime))
                throw new RowChangeSerializationException($"Column '{name}' of type '{tag}' must not carry a 'kind'.", name, tag);

            if (type == null)
            {
                if (hasValue && valueElement.ValueKind != JsonValueKind.Null)
                    throw new RowChangeSerializationException($"Column '{name}' of type 'null' must not carry a value.", name, tag);
                return new RowColumn(name, null);
            }

            if (!hasValue || valueElement.ValueKind != JsonValueKind.String)
                throw new RowChangeSerializationException($"Column '{name}' of type '{tag}' must carry its value as a JSON string.", name, tag);
            var text = valueElement.GetString();

            if (!TryParseValue(type, text, out var value))
                throw new RowChangeSerializationException($"Column '{name}': the value '{Truncate(text)}' does not parse as type '{tag}'.", name, tag);

            if (type == typeof(DateTime))
            {
                if (!hasKind || kindElement.ValueKind != JsonValueKind.String || !TryParseKind(kindElement.GetString(), out var kind))
                    throw new RowChangeSerializationException($"Column '{name}' of type 'datetime' must carry a kind of Utc, Local or Unspecified.", name, tag);
                value = DateTime.SpecifyKind((DateTime)value, kind);
            }
            return new RowColumn(name, value);
        }

        private static bool TryParseValue(Type type, string text, out object value)
        {
            var c = CultureInfo.InvariantCulture;
            const NumberStyles integer = NumberStyles.AllowLeadingSign;
            const NumberStyles real = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
            bool ok;
            value = null;
            if (type == typeof(string)) { value = text; return true; }
            if (type == typeof(bool))
            {
                ok = text == "true" || text == "false";
                if (ok) value = text == "true";
                return ok;
            }
            if (type == typeof(byte)) { ok = byte.TryParse(text, integer, c, out var v); value = v; return ok; }
            if (type == typeof(short)) { ok = short.TryParse(text, integer, c, out var v); value = v; return ok; }
            if (type == typeof(int)) { ok = int.TryParse(text, integer, c, out var v); value = v; return ok; }
            if (type == typeof(long)) { ok = long.TryParse(text, integer, c, out var v); value = v; return ok; }
            if (type == typeof(float)) { ok = float.TryParse(text, real, c, out var v); value = v; return ok; }
            if (type == typeof(double)) { ok = double.TryParse(text, real, c, out var v); value = v; return ok; }
            if (type == typeof(decimal)) { ok = decimal.TryParse(text, real, c, out var v); value = v; return ok; }
            if (type == typeof(Guid)) { ok = Guid.TryParseExact(text, "D", out var v); value = v; return ok; }
            if (type == typeof(DateTime)) { ok = DateTime.TryParseExact(text, DateTimeFormat, c, DateTimeStyles.None, out var v); value = v; return ok; }
            if (type == typeof(DateTimeOffset)) { ok = DateTimeOffset.TryParseExact(text, "O", c, DateTimeStyles.None, out var v); value = v; return ok; }
            if (type == typeof(TimeSpan)) { ok = TimeSpan.TryParseExact(text, "c", c, out var v); value = v; return ok; }
            if (type == typeof(byte[]))
            {
                var buffer = new byte[(text.Length * 3 + 3) / 4];
                ok = Convert.TryFromBase64String(text, buffer, out var written);
                if (ok) value = buffer.AsSpan(0, written).ToArray();
                return ok;
            }
            return false;
        }

        private static bool TryParseOperation(string text, out RowChangeOperation operation)
        {
            switch (text)
            {
                case "Insert": operation = RowChangeOperation.Insert; return true;
                case "Update": operation = RowChangeOperation.Update; return true;
                case "Delete": operation = RowChangeOperation.Delete; return true;
                default: operation = default; return false;
            }
        }

        private static bool TryParseKind(string text, out DateTimeKind kind)
        {
            switch (text)
            {
                case "Utc": kind = DateTimeKind.Utc; return true;
                case "Local": kind = DateTimeKind.Local; return true;
                case "Unspecified": kind = DateTimeKind.Unspecified; return true;
                default: kind = default; return false;
            }
        }

        #endregion

        private static string Describe(string context) => context == RowContext ? "Row change payload" : $"Column '{context}'";

        private static bool IsDefinedOperation(RowChangeOperation operation) =>
            operation == RowChangeOperation.Insert || operation == RowChangeOperation.Update || operation == RowChangeOperation.Delete;

        private static string AllowedList() => "null, " + string.Join(", ", RowValueTypes.Allowed.Select(t => t.Name));

        private static string AllowedTags() => string.Join(", ", RowValueTypes.Tags);

        private static string Truncate(string text) => text.Length <= 64 ? text : text.Substring(0, 64) + "...";
    }
}
