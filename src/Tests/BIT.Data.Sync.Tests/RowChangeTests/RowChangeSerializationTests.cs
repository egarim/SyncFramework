using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BIT.Data.Sync.Tests.RowChangeTests
{
    /// <summary>Records whether <see cref="PoisonPayloadType"/> was ever initialized; kept in a separate class so reading it does not run that type's static constructor.</summary>
    public static class PoisonFlags
    {
        public static bool PoisonTypeLoaded;
    }

    /// <summary>A type a hostile payload may name. Its static constructor flips a flag, so the tests can prove it was never loaded.</summary>
    public class PoisonPayloadType
    {
        static PoisonPayloadType()
        {
            PoisonFlags.PoisonTypeLoaded = true;
        }

        public string Payload { get; set; }
    }

    public class CustomValue
    {
        public int X { get; set; }
    }

    public class RowChangeSerializationTests
    {
        private const string PoisonTypeName = "BIT.Data.Sync.Tests.RowChangeTests.PoisonPayloadType, BIT.Data.Sync.Tests";

        private static readonly DateTime UtcDate = new DateTime(2024, 5, 1, 10, 20, 30, DateTimeKind.Utc).AddTicks(1234567);
        private static readonly DateTime LocalDate = new DateTime(2023, 3, 26, 2, 30, 0, DateTimeKind.Local).AddTicks(7);
        private static readonly DateTime UnspecifiedDate = new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Unspecified).AddTicks(9999999);

        /// <summary>One value per allowed type, by name, as written in the spec.</summary>
        private static IEnumerable<TestCaseData> AllowedValues()
        {
            yield return new TestCaseData(null, null).SetName("Allowed_null");
            yield return new TestCaseData("hello \"world\" é中", typeof(string)).SetName("Allowed_string");
            yield return new TestCaseData(true, typeof(bool)).SetName("Allowed_bool");
            yield return new TestCaseData(byte.MaxValue, typeof(byte)).SetName("Allowed_byte");
            yield return new TestCaseData(short.MinValue, typeof(short)).SetName("Allowed_short");
            yield return new TestCaseData(int.MinValue, typeof(int)).SetName("Allowed_int");
            yield return new TestCaseData(long.MaxValue, typeof(long)).SetName("Allowed_long");
            yield return new TestCaseData(float.Epsilon, typeof(float)).SetName("Allowed_float");
            yield return new TestCaseData(0.1 + 0.2, typeof(double)).SetName("Allowed_double");
            yield return new TestCaseData(1234.5600m, typeof(decimal)).SetName("Allowed_decimal");
            yield return new TestCaseData(Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff"), typeof(Guid)).SetName("Allowed_Guid");
            yield return new TestCaseData(UtcDate, typeof(DateTime)).SetName("Allowed_DateTime");
            yield return new TestCaseData(new DateTimeOffset(2024, 5, 1, 10, 20, 30, TimeSpan.FromHours(-5.5)).AddTicks(42), typeof(DateTimeOffset)).SetName("Allowed_DateTimeOffset");
            yield return new TestCaseData(TimeSpan.FromTicks(-123456789012345), typeof(TimeSpan)).SetName("Allowed_TimeSpan");
            yield return new TestCaseData(new byte[] { 0, 1, 2, 254, 255 }, typeof(byte[])).SetName("Allowed_byte_array");
        }

        private static RowChange BlogUpdate(IEnumerable<RowColumn> columns) => new RowChange
        {
            Table = "dbo.Blog",
            Operation = RowChangeOperation.Update,
            Keys = new List<RowColumn> { new RowColumn("Id", 42L), new RowColumn("Tenant", "a") },
            Columns = columns.ToList()
        };

        private static RowChange RoundTrip(RowChange rowChange) =>
            SerializationHelper.DeserializeCore<RowChange>(SerializationHelper.SerializeCore(rowChange));

        private static byte[] Utf8(string json) => Encoding.UTF8.GetBytes(json);

        private static string Payload(string columnJson) =>
            "{\"format\":\"bit.rowchange/1\",\"table\":\"dbo.Blog\",\"operation\":\"Update\",\"keys\":[],\"columns\":[" + columnJson + "]}";

        private static RowChangeSerializationException Refused(string json) =>
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.DeserializeCore<RowChange>(Utf8(json)));

        [Test]
        public void AllowedList_IsTheClosedListNamedInTheSpec()
        {
            var expected = new[]
            {
                typeof(string), typeof(bool), typeof(byte), typeof(short), typeof(int), typeof(long), typeof(float),
                typeof(double), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset), typeof(TimeSpan), typeof(byte[])
            };
            CollectionAssert.AreEquivalent(expected, RowValueTypes.Allowed);
            Assert.That(RowValueTypes.Tags, Does.Contain(RowValueTypes.NullTag));
            Assert.That(RowValueTypes.Tags.Count, Is.EqualTo(expected.Length + 1));
            Assert.That(RowValueTypes.IsAllowed(null), Is.True);
        }

        [TestCaseSource(nameof(AllowedValues))]
        public void EachAllowedType_RoundTripsWithItsExactClrType(object value, Type expectedType)
        {
            var result = RoundTrip(BlogUpdate(new[] { new RowColumn("Value", value) }));

            var actual = result.Columns.Single().Value;
            if (expectedType == null)
                Assert.That(actual, Is.Null);
            else
                Assert.That(actual.GetType(), Is.EqualTo(expectedType));
            Assert.That(RowColumn.Equals(result.Columns.Single(), new RowColumn("Value", value)), Is.True);
        }

        [Test]
        public void IntColumn_ComesBackAsInt_NotLong()
        {
            var result = RoundTrip(BlogUpdate(new[] { new RowColumn("Count", 7), new RowColumn("Small", (short)7), new RowColumn("Tiny", (byte)7) }));
            Assert.That(result.Columns[0].Value, Is.TypeOf<int>().And.EqualTo(7));
            Assert.That(result.Columns[1].Value, Is.TypeOf<short>());
            Assert.That(result.Columns[2].Value, Is.TypeOf<byte>());
        }

        [Test]
        public void BlogUpdate_WithEveryAllowedType_IsEqualFieldByField()
        {
            var bytes = new byte[] { 0x00, 0xFF, 0x10, 0x80, 0x7F };
            var original = BlogUpdate(new[]
            {
                new RowColumn("NullValue", null),
                new RowColumn("EmptyText", ""),
                new RowColumn("Text", "x"),
                new RowColumn("Flag", false),
                new RowColumn("Byte", (byte)3),
                new RowColumn("Short", (short)-3),
                new RowColumn("Int", 123),
                new RowColumn("Long", long.MaxValue),
                new RowColumn("LongMin", long.MinValue),
                new RowColumn("Float", 3.14159274f),
                new RowColumn("FloatNaN", float.NaN),
                new RowColumn("Double", Math.PI),
                new RowColumn("DoubleNaN", double.NaN),
                new RowColumn("DoublePosInf", double.PositiveInfinity),
                new RowColumn("DoubleNegInf", double.NegativeInfinity),
                new RowColumn("Decimal", 1234.5600m),
                new RowColumn("DecimalMax", decimal.MaxValue),
                new RowColumn("Guid", Guid.NewGuid()),
                new RowColumn("Utc", UtcDate),
                new RowColumn("Local", LocalDate),
                new RowColumn("Unspecified", UnspecifiedDate),
                new RowColumn("Offset", new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(9))),
                new RowColumn("Span", new TimeSpan(1, 2, 3, 4, 5)),
                new RowColumn("Bytes", bytes),
                new RowColumn("EmptyBytes", new byte[0]),
            });

            var payload = SerializationHelper.SerializeCore(original);
            var result = SerializationHelper.DeserializeCore<RowChange>(payload);

            Assert.That(result, Is.Not.SameAs(original));
            Assert.That(result.Table, Is.EqualTo("dbo.Blog"));
            Assert.That(result.Operation, Is.EqualTo(RowChangeOperation.Update));
            Assert.That(result.Keys.Count, Is.EqualTo(2));
            Assert.That(result.Keys[0].Name, Is.EqualTo("Id"));
            Assert.That(result.Keys[0].Value, Is.TypeOf<long>().And.EqualTo(42L));
            Assert.That(result.Keys[1].Name, Is.EqualTo("Tenant"));
            Assert.That(result.Keys[1].Value, Is.TypeOf<string>().And.EqualTo("a"));
            Assert.That(result.Columns.Count, Is.EqualTo(original.Columns.Count));
            for (int i = 0; i < original.Columns.Count; i++)
                Assert.That(result.Columns[i], Is.EqualTo(original.Columns[i]), original.Columns[i].Name);
            Assert.That(result, Is.EqualTo(original));

            object Col(string name) => result.Columns.Single(c => c.Name == name).Value;

            Assert.That(((DateTime)Col("Utc")).Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(((DateTime)Col("Utc")).Ticks, Is.EqualTo(UtcDate.Ticks));
            Assert.That(((DateTime)Col("Local")).Kind, Is.EqualTo(DateTimeKind.Local));
            Assert.That(((DateTime)Col("Local")).Ticks, Is.EqualTo(LocalDate.Ticks));
            Assert.That(((DateTime)Col("Unspecified")).Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(((decimal)Col("Decimal")).ToString(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("1234.5600"));
            Assert.That(decimal.GetBits((decimal)Col("Decimal")), Is.EqualTo(decimal.GetBits(1234.5600m)));
            Assert.That(Col("Long"), Is.EqualTo(long.MaxValue));
            Assert.That(double.IsNaN((double)Col("DoubleNaN")), Is.True);
            Assert.That(float.IsNaN((float)Col("FloatNaN")), Is.True);
            Assert.That(Col("DoublePosInf"), Is.EqualTo(double.PositiveInfinity));
            Assert.That(Col("DoubleNegInf"), Is.EqualTo(double.NegativeInfinity));
            Assert.That(Col("NullValue"), Is.Null);
            Assert.That(Col("EmptyText"), Is.TypeOf<string>().And.EqualTo(""));
            Assert.That(Col("Bytes"), Is.EqualTo(bytes));
            Assert.That(Col("Bytes"), Is.Not.SameAs(bytes));
            Assert.That(Col("EmptyBytes"), Is.TypeOf<byte[]>().And.Empty);
            Assert.That(((DateTimeOffset)Col("Offset")).Offset, Is.EqualTo(TimeSpan.FromHours(9)));
        }

        [Test]
        public void NullAndEmptyString_StayDistinct()
        {
            var result = RoundTrip(BlogUpdate(new[] { new RowColumn("A", null), new RowColumn("B", "") }));
            Assert.That(result.Columns[0].Value, Is.Null);
            Assert.That(result.Columns[1].Value, Is.Not.Null.And.EqualTo(""));
        }

        [Test]
        public void ListOfRowChanges_RoundTripsThroughSerializationHelper()
        {
            var list = new List<RowChange>
            {
                BlogUpdate(new[] { new RowColumn("Title", "t") }),
                new RowChange { Table = "dbo.Post", Operation = RowChangeOperation.Insert, Keys = { new RowColumn("Id", 1) } },
                new RowChange { Table = "dbo.Post", Operation = RowChangeOperation.Delete, Keys = { new RowColumn("Id", 2) } },
            };
            var payload = SerializationHelper.SerializeCore(list);

            Assert.That(SerializationHelper.DeserializeCore<List<RowChange>>(payload), Is.EqualTo(list));
            Assert.That(SerializationHelper.DeserializeCore<RowChange[]>(payload), Is.EqualTo(list));
            Assert.That(SerializationHelper.DeserializeCore<IEnumerable<RowChange>>(payload), Is.EqualTo(list));
        }

        [Test]
        public void RowChange_SurvivesCompressionInsideADelta()
        {
            var original = BlogUpdate(new[] { new RowColumn("Rating", 4.5m) });
            var delta = SerializationHelper.CreateDeltaCore("client", original);
            var result = SerializationHelper.DeserializeCore<RowChange>(SerializationHelper.DecompressCore(delta.Operation));
            Assert.That(result, Is.EqualTo(original));
        }

        [TestCaseSource(nameof(DisallowedValues))]
        public void Serializing_DisallowedValueType_Throws_NamingColumnAndType(object value)
        {
            var rowChange = BlogUpdate(new[] { new RowColumn("Fine", 1), new RowColumn("Bad", value) });
            byte[] payload = null;

            var ex = Assert.Throws<RowChangeSerializationException>(() => payload = SerializationHelper.SerializeCore(rowChange));

            Assert.That(payload, Is.Null, "no partial payload is returned");
            Assert.That(ex.ColumnName, Is.EqualTo("Bad"));
            Assert.That(ex.RejectedType, Is.EqualTo(value.GetType().FullName));
            Assert.That(ex.Message, Does.Contain("Bad"));
            Assert.That(ex.Message, Does.Contain(value.GetType().FullName));
        }

        private static IEnumerable<TestCaseData> DisallowedValues()
        {
            yield return new TestCaseData(new CustomValue { X = 1 }).SetName("Disallowed_custom_class");
            yield return new TestCaseData(new List<int> { 1, 2 }).SetName("Disallowed_List_of_int");
            yield return new TestCaseData(5u).SetName("Disallowed_uint");
            yield return new TestCaseData('c').SetName("Disallowed_char");
            yield return new TestCaseData(RowChangeOperation.Insert).SetName("Disallowed_enum");
            yield return new TestCaseData(new object()).SetName("Disallowed_object");
        }

        [Test]
        public void Serializing_DisallowedKeyValue_Throws()
        {
            var rowChange = BlogUpdate(new RowColumn[0]);
            rowChange.Keys.Add(new RowColumn("Weird", new CustomValue()));
            var ex = Assert.Throws<RowChangeSerializationException>(() => RowChangeSerializer.Serialize(rowChange));
            Assert.That(ex.ColumnName, Is.EqualTo("Weird"));
        }

        [Test]
        public void Serializing_ListWithOneBadRow_ReturnsNothing()
        {
            var list = new List<RowChange> { BlogUpdate(new[] { new RowColumn("A", 1) }), BlogUpdate(new[] { new RowColumn("B", new List<int>()) }) };
            byte[] payload = null;
            Assert.Throws<RowChangeSerializationException>(() => payload = SerializationHelper.SerializeCore(list));
            Assert.That(payload, Is.Null);
        }

        [Test]
        public void Serializing_IncompleteRowChange_Throws()
        {
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.SerializeCore(new RowChange { Operation = RowChangeOperation.Insert }));
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.SerializeCore(new RowChange { Table = "t", Operation = (RowChangeOperation)99 }));
        }

        [TestCase(PoisonTypeName)]
        [TestCase("System.Diagnostics.Process, System")]
        [TestCase("System.Int32")]
        [TestCase("System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e")]
        [TestCase("Newtonsoft.Json.Converters.StringEnumConverter")]
        [TestCase("Int")]
        [TestCase("int ")]
        [TestCase("")]
        public void Deserializing_UnknownTypeTag_IsRefused_AndNeverLoaded(string tag)
        {
            var json = Payload("{\"name\":\"Evil\",\"type\":" + JsonString(tag) + ",\"value\":\"x\"}");

            var ex = Refused(json);

            Assert.That(ex.ColumnName, Is.EqualTo("Evil"));
            Assert.That(ex.RejectedType, Is.EqualTo(tag));
            Assert.That(ex.Message, Does.Contain("Evil"));
            Assert.That(ex.Message, Does.Contain("'" + tag + "'"));
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False, "the type named in the payload must never be loaded");
        }

        [TestCase("$type")]
        [TestCase("__type")]
        [TestCase("converter")]
        [TestCase("$converter")]
        public void Deserializing_TypeOrConverterMemberOnColumn_IsRefused_AndNeverLoaded(string member)
        {
            var json = Payload("{\"" + member + "\":\"" + PoisonTypeName + "\",\"name\":\"Evil\",\"type\":\"string\",\"value\":\"x\"}");

            var ex = Refused(json);

            Assert.That(ex.ColumnName, Is.EqualTo("Evil"));
            Assert.That(ex.RejectedType, Is.EqualTo(member));
            Assert.That(ex.Message, Does.Contain("Evil"));
            Assert.That(ex.Message, Does.Contain(member));
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False);
        }

        [TestCase("$type")]
        [TestCase("__type")]
        [TestCase("converter")]
        public void Deserializing_TypeMemberOnRowChange_IsRefused_AndNeverLoaded(string member)
        {
            var json = "{\"" + member + "\":\"" + PoisonTypeName + "\"," + Payload("").Substring(1);

            var ex = Refused(json);

            Assert.That(ex.RejectedType, Is.EqualTo(member));
            Assert.That(ex.Message, Does.Contain(member));
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False);
        }

        [Test]
        public void Deserializing_TypeTagAsObject_IsRefused()
        {
            var ex = Refused(Payload("{\"name\":\"Evil\",\"type\":{\"$type\":\"" + PoisonTypeName + "\"},\"value\":\"x\"}"));
            Assert.That(ex.ColumnName, Is.EqualTo("Evil"));
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False);
        }

        [Test]
        public void Deserializing_ListPayloadWithPoison_IsRefused()
        {
            var json = "[" + Payload("{\"name\":\"Evil\",\"type\":\"" + PoisonTypeName + "\",\"value\":\"x\"}") + "]";
            var ex = Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.DeserializeCore<List<RowChange>>(Utf8(json)));
            Assert.That(ex.ColumnName, Is.EqualTo("Evil"));
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False);
        }

        [Test]
        public void Deserializing_MissingTable_IsRefused()
        {
            var ex = Refused("{\"format\":\"bit.rowchange/1\",\"operation\":\"Update\",\"keys\":[],\"columns\":[]}");
            Assert.That(ex.Message, Does.Contain("table"));
        }

        [Test]
        public void Deserializing_NullOrEmptyTable_IsRefused()
        {
            Refused("{\"format\":\"bit.rowchange/1\",\"table\":null,\"operation\":\"Update\",\"keys\":[],\"columns\":[]}");
            Refused("{\"format\":\"bit.rowchange/1\",\"table\":\"\",\"operation\":\"Update\",\"keys\":[],\"columns\":[]}");
            Refused("{\"format\":\"bit.rowchange/1\",\"table\":42,\"operation\":\"Update\",\"keys\":[],\"columns\":[]}");
        }

        [Test]
        public void Deserializing_MissingOperation_IsRefused()
        {
            var ex = Refused("{\"format\":\"bit.rowchange/1\",\"table\":\"dbo.Blog\",\"keys\":[],\"columns\":[]}");
            Assert.That(ex.Message, Does.Contain("operation"));
        }

        [TestCase("\"Upsert\"")]
        [TestCase("\"update\"")]
        [TestCase("\"1\"")]
        [TestCase("\"Insert, Update\"")]
        [TestCase("1")]
        [TestCase("null")]
        public void Deserializing_OperationOutsideInsertUpdateDelete_IsRefused(string operation)
        {
            Refused("{\"format\":\"bit.rowchange/1\",\"table\":\"dbo.Blog\",\"operation\":" + operation + ",\"keys\":[],\"columns\":[]}");
        }

        [TestCase("int", "abc")]
        [TestCase("int", "2147483648")]
        [TestCase("int", "1.0")]
        [TestCase("byte", "256")]
        [TestCase("byte", "-1")]
        [TestCase("short", "40000")]
        [TestCase("long", "9223372036854775808")]
        [TestCase("long", "0x10")]
        [TestCase("bool", "True")]
        [TestCase("bool", "1")]
        [TestCase("decimal", "1,5")]
        [TestCase("decimal", "abc")]
        [TestCase("double", "")]
        [TestCase("float", "pi")]
        [TestCase("guid", "not-a-guid")]
        [TestCase("datetime", "2024-13-01T00:00:00.0000000")]
        [TestCase("datetimeoffset", "yesterday")]
        [TestCase("timespan", "1 hour")]
        [TestCase("bytes", "***")]
        public void Deserializing_ValueThatDoesNotParseAsItsType_IsRefused(string tag, string text)
        {
            var kind = tag == "datetime" ? ",\"kind\":\"Utc\"" : "";
            var ex = Refused(Payload("{\"name\":\"Col\",\"type\":\"" + tag + "\",\"value\":" + JsonString(text) + kind + "}"));
            Assert.That(ex.ColumnName, Is.EqualTo("Col"));
            Assert.That(ex.RejectedType, Is.EqualTo(tag));
            Assert.That(ex.Message, Does.Contain("Col"));
        }

        [TestCase("{\"name\":\"Col\",\"type\":\"int\"}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"int\",\"value\":5}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"int\",\"value\":null}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"string\",\"value\":null}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"null\",\"value\":\"x\"}")]
        [TestCase("{\"name\":\"Col\",\"value\":\"x\"}")]
        [TestCase("{\"type\":\"int\",\"value\":\"1\"}")]
        [TestCase("{\"name\":\"\",\"type\":\"int\",\"value\":\"1\"}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"datetime\",\"value\":\"2024-01-01T00:00:00.0000000\"}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"datetime\",\"value\":\"2024-01-01T00:00:00.0000000\",\"kind\":\"Zulu\"}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"int\",\"value\":\"1\",\"kind\":\"Utc\"}")]
        [TestCase("{\"name\":\"Col\",\"type\":\"int\",\"value\":\"1\",\"value\":\"2\"}")]
        [TestCase("\"Col\"")]
        [TestCase("null")]
        public void Deserializing_MalformedColumn_IsRefused(string column)
        {
            Refused(Payload(column));
        }

        [TestCase("")]
        [TestCase("not json")]
        [TestCase("null")]
        [TestCase("[]")]
        [TestCase("{}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"operation\":\"Insert\",\"keys\":{},\"columns\":[]}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"operation\":\"Insert\",\"columns\":[]}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"operation\":\"Insert\",\"keys\":[]}")]
        [TestCase("{\"format\":\"other/9\",\"table\":\"t\",\"operation\":\"Insert\",\"keys\":[],\"columns\":[]}")]
        [TestCase("{\"table\":\"t\",\"operation\":\"Insert\",\"keys\":[],\"columns\":[]}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"table\":\"u\",\"operation\":\"Insert\",\"keys\":[],\"columns\":[]}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"operation\":\"Insert\",\"keys\":[],\"columns\":[],}")]
        [TestCase("{\"format\":\"bit.rowchange/1\",\"table\":\"t\",\"operation\":\"Insert\",\"keys\":[],\"columns\":[]")]
        public void Deserializing_MalformedPayload_IsRefused(string json)
        {
            Refused(json);
        }

        [Test]
        public void Deserializing_NullOrInvalidUtf8_IsRefused()
        {
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.DeserializeCore<RowChange>(null));
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.DeserializeCore<RowChange>(new byte[] { 0x7B, 0xFF, 0xFE, 0x7D }));
            Assert.Throws<RowChangeSerializationException>(() => SerializationHelper.DeserializeCore<List<RowChange>>(Utf8(Payload(""))));
        }

        [Test]
        public void Deserializing_ValidHandWrittenPayload_Works()
        {
            var json = "{\"format\":\"bit.rowchange/1\",\"table\":\"dbo.Blog\",\"operation\":\"Delete\"," +
                       "\"keys\":[{\"name\":\"Id\",\"type\":\"long\",\"value\":\"42\"},{\"name\":\"Tenant\",\"type\":\"string\",\"value\":\"a\"}]," +
                       "\"columns\":[{\"name\":\"Gone\",\"type\":\"null\"}]}";
            var result = SerializationHelper.DeserializeCore<RowChange>(Utf8(json));
            Assert.That(result, Is.EqualTo(new RowChange
            {
                Table = "dbo.Blog",
                Operation = RowChangeOperation.Delete,
                Keys = { new RowColumn("Id", 42L), new RowColumn("Tenant", "a") },
                Columns = { new RowColumn("Gone", null) }
            }));
        }

        [Test]
        public void Equality_IsStrictAboutTypeScaleAndKind()
        {
            Assert.That(new RowColumn("a", 1), Is.Not.EqualTo(new RowColumn("a", 1L)));
            Assert.That(new RowColumn("a", 1.50m), Is.Not.EqualTo(new RowColumn("a", 1.5m)));
            Assert.That(new RowColumn("a", DateTime.SpecifyKind(UtcDate, DateTimeKind.Local)), Is.Not.EqualTo(new RowColumn("a", UtcDate)));
            Assert.That(new RowColumn("a", null), Is.Not.EqualTo(new RowColumn("a", "")));
            Assert.That(new RowColumn("a", new byte[] { 1 }), Is.EqualTo(new RowColumn("a", new byte[] { 1 })));
            Assert.That(new RowColumn("a", new byte[] { 1 }), Is.Not.EqualTo(new RowColumn("a", new byte[] { 2 })));
        }

        [OneTimeTearDown]
        public void PoisonTypeWasNeverLoaded()
        {
            Assert.That(PoisonFlags.PoisonTypeLoaded, Is.False);
        }

        private static string JsonString(string value) =>
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
