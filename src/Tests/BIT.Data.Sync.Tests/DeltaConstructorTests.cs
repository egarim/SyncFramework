using BIT.Data.Sync.Imp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests
{
    /// <summary>
    /// Every public Delta constructor leaves a delta that a delta store can save and a sync client can push (T-11).
    /// </summary>
    public class DeltaConstructorTests
    {
        const string SampleIdentity = "node-a";
        const string SampleIndex = "index-1";
        static readonly byte[] SampleOperation = { 1, 2, 3 };

        static void AssertReadyToSaveAndSend(IDelta delta)
        {
            Assert.AreNotEqual(default(DateTime), delta.Date, "Date");
            Assert.AreNotEqual(0d, delta.Epoch, "Epoch");
            Assert.IsFalse(string.IsNullOrEmpty(delta.DeltaId), "DeltaId");
        }

        static void AssertSameValues(IDelta expected, IDelta actual)
        {
            Assert.AreEqual(expected.DeltaId, actual.DeltaId, "DeltaId");
            Assert.AreEqual(expected.Date, actual.Date, "Date");
            Assert.AreEqual(expected.Epoch, actual.Epoch, "Epoch");
            Assert.AreEqual(expected.Identity, actual.Identity, "Identity");
            Assert.AreEqual(expected.Index, actual.Index, "Index");
            Assert.AreEqual(expected.Operation, actual.Operation, "Operation");
        }

        static IDelta SourceDelta()
        {
            return new SourceDeltaStub
            {
                DeltaId = "source-delta-id",
                Date = new DateTime(2024, 5, 6, 7, 8, 9, 123, DateTimeKind.Utc),
                Epoch = 1714979289123d,
                Identity = SampleIdentity,
                Index = SampleIndex,
                Operation = SampleOperation,
            };
        }

        static IEnumerable<TestCaseData> ConstructorsWithoutSource()
        {
            yield return new TestCaseData(new Func<Delta>(() => new Delta())).SetName("{m}(Delta())");
            yield return new TestCaseData(new Func<Delta>(() => new Delta(SampleIdentity, SampleOperation))).SetName("{m}(Delta(identity, operation))");
            yield return new TestCaseData(new Func<Delta>(() => new Delta(SampleIdentity, SampleIndex, SampleOperation))).SetName("{m}(Delta(identity, index, operation))");
        }

        static IEnumerable<TestCaseData> EveryConstructor()
        {
            foreach (var testCase in ConstructorsWithoutSource())
                yield return testCase;
            yield return new TestCaseData(new Func<Delta>(() => new Delta(SourceDelta()))).SetName("{m}(Delta(IDelta))");
        }

        [Test]
        public void PublicConstructors_AreTheSameAsOnMain()
        {
            var actual = typeof(Delta).GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .Select(c => string.Join(",", c.GetParameters().Select(p => p.ParameterType.Name)))
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
            var expected = new[] { "", "IDelta", "String,Byte[]", "String,String,Byte[]" }
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ExistingCallers_StillCompile()
        {
            // Calls written the way code on main calls each constructor, including named arguments.
            IDelta source = new Delta();
            var deltas = new[]
            {
                new Delta(),
                new Delta(identity: SampleIdentity, operation: SampleOperation),
                new Delta(identity: SampleIdentity, index: SampleIndex, operation: SampleOperation),
                new Delta(Delta: source),
                new Delta { Identity = SampleIdentity, Operation = SampleOperation },
            };

            Assert.AreEqual(5, deltas.Length);
        }

        [Test]
        public void DefaultConstructor_SetsSharedDefaults()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var delta = new Delta();
            var after = DateTime.UtcNow.AddSeconds(1);

            AssertReadyToSaveAndSend(delta);
            Assert.IsNull(delta.Identity);
            Assert.IsNull(delta.Index);
            Assert.IsNull(delta.Operation);
            Assert.That(delta.Date, Is.InRange(before, after));
            Assert.AreEqual(DateTimeKind.Utc, delta.Date.Kind);
            Assert.AreEqual((delta.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, delta.Epoch);
        }

        [Test]
        public void IdentityOperationConstructor_KeepsInputsAndSetsDefaults()
        {
            var delta = new Delta(SampleIdentity, new byte[] { 1, 2, 3 });

            AssertReadyToSaveAndSend(delta);
            Assert.AreEqual(SampleIdentity, delta.Identity);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, delta.Operation);
            Assert.IsNull(delta.Index);
            Assert.AreEqual((delta.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, delta.Epoch);
        }

        [Test]
        public void IdentityIndexOperationConstructor_KeepsInputsAndSetsDefaults()
        {
            var delta = new Delta(SampleIdentity, SampleIndex, new byte[] { 1, 2, 3 });

            AssertReadyToSaveAndSend(delta);
            Assert.AreEqual(SampleIdentity, delta.Identity);
            Assert.AreEqual(SampleIndex, delta.Index);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, delta.Operation);
            Assert.AreEqual((delta.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, delta.Epoch);
        }

        [Test]
        public void CopyConstructor_CopiesEveryIDeltaProperty()
        {
            var source = SourceDelta();

            var delta = new Delta(source);

            AssertSameValues(source, delta);
            AssertReadyToSaveAndSend(delta);
            Assert.IsNotNull(delta.Identity);
            Assert.IsNotNull(delta.Index);
            Assert.IsNotNull(delta.Operation);
        }

        [Test]
        public void CopyConstructor_CopiesFromAnotherDelta()
        {
            var source = new Delta(SampleIdentity, SampleIndex, SampleOperation);

            var delta = new Delta(source);

            AssertSameValues(source, delta);
        }

        // The copy constructor keeps the source DeltaId on purpose; TwoDeltasFromCopyConstructor_AreBothStored covers it.
        [TestCaseSource(nameof(ConstructorsWithoutSource))]
        public void SameConstructorTwice_GivesDifferentDeltaIds(Func<Delta> create)
        {
            var first = create();
            var second = create();

            Assert.AreNotEqual(first.DeltaId, second.DeltaId);
        }

        [Test]
        public async Task TwoDeltasFromIdentityOperationConstructor_AreBothStored()
        {
            await AssertBothStored(new Delta(SampleIdentity, new byte[] { 1, 2, 3 }), new Delta(SampleIdentity, new byte[] { 4, 5, 6 }));
        }

        [Test]
        public async Task TwoDeltasFromIdentityIndexOperationConstructor_AreBothStored()
        {
            await AssertBothStored(new Delta(SampleIdentity, SampleIndex, new byte[] { 1, 2, 3 }), new Delta(SampleIdentity, SampleIndex, new byte[] { 4, 5, 6 }));
        }

        [Test]
        public async Task TwoDeltasFromDefaultConstructor_AreBothStored()
        {
            await AssertBothStored(
                new Delta { Identity = SampleIdentity, Operation = new byte[] { 1, 2, 3 } },
                new Delta { Identity = SampleIdentity, Operation = new byte[] { 4, 5, 6 } });
        }

        [Test]
        public async Task TwoDeltasFromCopyConstructor_AreBothStored()
        {
            await AssertBothStored(
                new Delta(new Delta(SampleIdentity, new byte[] { 1, 2, 3 })),
                new Delta(new Delta(SampleIdentity, new byte[] { 4, 5, 6 })));
        }

        static async Task AssertBothStored(Delta first, Delta second)
        {
            Assert.AreNotEqual(first.DeltaId, second.DeltaId);
            var expectedOperations = new[] { first.Operation, second.Operation };
            var expectedIds = new[] { first.DeltaId, second.DeltaId };
            IDeltaStore store = new MemoryDeltaStore(new List<IDelta>());

            await store.SaveDeltasAsync(new List<IDelta> { first, second }, default);

            var stored = (await store.GetDeltasAsync(string.Empty, default)).ToList();
            Assert.AreEqual(2, stored.Count);
            Assert.AreEqual(expectedOperations, stored.Select(d => d.Operation).ToArray());
            Assert.AreEqual(expectedIds, stored.Select(d => d.DeltaId).ToArray());
        }

        [TestCaseSource(nameof(EveryConstructor))]
        public void SerializationRoundTrip_KeepsEveryIDeltaProperty(Func<Delta> create)
        {
            var delta = create();

            var data = SerializationHelper.SerializeCore(delta);
            var back = SerializationHelper.DeserializeCore<Delta>(data);

            AssertSameValues(delta, back);
        }

        [Test]
        public void SerializedShape_HasTheSameMembers()
        {
            var json = System.Text.Encoding.UTF8.GetString(SerializationHelper.SerializeCore(new Delta(SampleIdentity, SampleIndex, SampleOperation)));

            foreach (var member in new[] { "Date", "DeltaId", "Epoch", "Identity", "Index", "Operation" })
                StringAssert.Contains("\"" + member + "\":", json);
            var dataMembers = typeof(Delta).GetProperties()
                .Where(p => p.GetCustomAttribute<System.Runtime.Serialization.DataMemberAttribute>() != null)
                .Select(p => p.Name)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
            Assert.AreEqual(new[] { "Date", "DeltaId", "Epoch", "Identity", "Index", "Operation" }, dataMembers);
        }

        class SourceDeltaStub : IDelta
        {
            public string DeltaId { get; set; }
            public DateTime Date { get; set; }
            public double Epoch { get; set; }
            public string Identity { get; set; }
            public string Index { get; set; }
            public byte[] Operation { get; set; }
        }
    }
}
