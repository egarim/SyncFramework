using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BIT.Data.Sync
{
    public class ConnectionStringParserService
    {
        struct ValuePair
        {
            public readonly string OriginalValue;
            public readonly string Value;
            public ValuePair(string originalValue, string value)
            {
                OriginalValue = originalValue;
                Value = value;
            }
        }
        enum InValueState
        {
            None,
            InDoubleQuotedValue,
            InSingleQuotedValue
        }
        const string DoubleQuotesString = "\"";
        const char DoubleQuotesChar = '\"';
        private const string DoubleQuotesInValueString = "\"\"";
        const string SingleQuoteString = "'";
        const char SingleQuoteChar = '\'';
        private const string SingleQuoteInValueString = "''";
        Dictionary<string, ValuePair> propertyTable = new Dictionary<string, ValuePair>();
        string[] ExtractParts(string connectionString)
        {
            List<string> list = new List<string>();
            int count = connectionString.Length;
            int lastPos = 0;
            InValueState inValue = InValueState.None;
            bool waitForEnd = false;
            for (int i = 0; i < count; ++i)
            {
                switch (connectionString[i])
                {
                    case DoubleQuotesChar:
                        {
                            if (inValue == InValueState.InDoubleQuotedValue && ((i + 1) < count) && connectionString[i + 1].Equals(DoubleQuotesChar))
                            {
                                i += 1;
                            }
                            else
                            {
                                if (inValue == InValueState.InDoubleQuotedValue)
                                {
                                    waitForEnd = true;
                                    inValue = InValueState.None;
                                }
                                if (inValue == InValueState.None && !waitForEnd)
                                {
                                    inValue = InValueState.InDoubleQuotedValue;
                                }
                            }
                        }
                        break;
                    case SingleQuoteChar:
                        {
                            if (inValue == InValueState.InSingleQuotedValue && ((i + 1) < count) && connectionString[i + 1].Equals(SingleQuoteChar))
                            {
                                i += 1;
                            }
                            else
                            {
                                if (inValue == InValueState.InSingleQuotedValue)
                                {
                                    waitForEnd = true;
                                    inValue = InValueState.None;
                                }
                                if (inValue == InValueState.None && !waitForEnd)
                                {
                                    inValue = InValueState.InSingleQuotedValue;
                                }
                            }
                        }
                        break;
                    case ';':
                        if (inValue == InValueState.None)
                        {
                            var lastPart = connectionString.Substring(lastPos, (i - lastPos));
                            list.Add(lastPart);
                            lastPos = i + 1;
                            waitForEnd = false;
                        }
                        break;
                }
            }
            if (lastPos < count)
            {
                var lastPart = connectionString.Substring(lastPos, (count - lastPos));
                list.Add(inValue == InValueState.InDoubleQuotedValue ? (lastPart + DoubleQuotesString) : (inValue == InValueState.InSingleQuotedValue ? (lastPart + SingleQuoteString) : lastPart));
            }
            return list.ToArray();
        }
        public ConnectionStringParserService()
        {
        }
        public ConnectionStringParserService(string connectionString)
            : this()
        {
            string[] parts = ExtractParts(connectionString);
            for (int i = 0; i < parts.Length; ++i)
            {
                string text = parts[i];
                int ind = text.IndexOf("=");
                if (ind != -1)
                {
                    string name = text.Substring(0, ind).Trim();
                    string value = text.Substring(ind + 1);
                    this.propertyTable.Add(name, new ValuePair(value, UnescapeArgument(value.Trim())));
                }
            }
        }
        static string UnescapeArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (IsStringDoubleQuoted(value))
            {
                return value.Substring(1, value.Length - 2).Replace(DoubleQuotesInValueString, DoubleQuotesString);
            }
            if (IsStringSingleQuoted(value))
            {
                return value.Substring(1, value.Length - 2).Replace(SingleQuoteInValueString, SingleQuoteString);
            }
            return value;
        }
        static bool IsStringDoubleQuoted(string value)
        {
            return value.Length > 1 && value[0].Equals(DoubleQuotesChar) && value[value.Length - 1].Equals(DoubleQuotesChar);
        }
        static bool IsStringSingleQuoted(string value)
        {
            return value.Length > 1 && value[0].Equals(SingleQuoteChar) && value[value.Length - 1].Equals(SingleQuoteChar);
        }
        public static string EscapeArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            if (IsStringDoubleQuoted(value) || IsStringSingleQuoted(value))
            {
                return value;
            }
            if (value.Contains(";") || value.Contains(" ") || value.Contains(SingleQuoteString) || value.Contains(DoubleQuotesString))
            {
                return string.Concat(DoubleQuotesString, value.Replace(DoubleQuotesString, DoubleQuotesInValueString), DoubleQuotesString);
            }
            return value;
        }
        static string EscapeArgument(string originalValue, string value)
        {
            if (string.IsNullOrEmpty(originalValue))
            {
                return originalValue;
            }
            if (originalValue != value)
            {
                return originalValue;
            }
            return EscapeArgument(originalValue);
        }
        public string GetPartByName(string partName)
        {
            ValuePair s;
            return propertyTable.TryGetValue(partName, out s) ? s.Value : string.Empty;
        }
        public string GetOriginalPartByName(string partName)
        {
            ValuePair s;
            return propertyTable.TryGetValue(partName, out s) ? s.OriginalValue : string.Empty;
        }
        public bool PartExists(string partName)
        {
            return propertyTable.ContainsKey(partName);
        }
        public void AddPart(string partName, string partValue)
        {
            propertyTable.Add(partName, new ValuePair(partValue, partValue));
        }
        public void UpdatePartByName(string partName, string partValue)
        {
            if (propertyTable.ContainsKey(partName))
            {
                propertyTable[partName] = new ValuePair(partValue, partValue);
            }
        }
        public void RemovePartByName(string partName)
        {
            propertyTable.Remove(partName);
        }
        public string GetConnectionString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, ValuePair> de in propertyTable)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}", de.Key, EscapeArgument(de.Value.OriginalValue, de.Value.Value));
                builder.Append(";");
            }
            return builder.ToString();
        }
    }
}