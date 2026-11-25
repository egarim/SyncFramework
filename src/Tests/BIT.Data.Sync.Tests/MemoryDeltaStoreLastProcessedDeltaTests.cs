using NUnit.Framework;
using System.Collections.Generic;
using BIT.Data.Sync.Imp;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests
{
    /// <summary>
    /// The last processed delta is kept per identity: setting it for one identity never changes it for another,
    /// and saving deltas never changes it at all.
    /// </summary>
    public class MemoryDeltaStoreLastProcessedDeltaTests
    {
        const string IdentityA = "identityA";
        const string IdentityB = "identityB";

        [Test]
        public async Task EachIdentityReturnsItsOwnLastProcessedDelta()
        {
            IDeltaStore store = new MemoryDeltaStore();

            await store.SetLastProcessedDeltaAsync("A1", IdentityA, default);
            await store.SetLastProcessedDeltaAsync("B1", IdentityB, default);

            Assert.AreEqual("A1", await store.GetLastProcessedDeltaAsync(IdentityA, default));
            Assert.AreEqual("B1", await store.GetLastProcessedDeltaAsync(IdentityB, default));
        }

        [Test]
        public async Task UpdatingOneIdentityLeavesTheOtherUnchanged()
        {
            IDeltaStore store = new MemoryDeltaStore();
            await store.SetLastProcessedDeltaAsync("A1", IdentityA, default);
            await store.SetLastProcessedDeltaAsync("B1", IdentityB, default);

            await store.SetLastProcessedDeltaAsync("A2", IdentityA, default);

            Assert.AreEqual("A2", await store.GetLastProcessedDeltaAsync(IdentityA, default));
            Assert.AreEqual("B1", await store.GetLastProcessedDeltaAsync(IdentityB, default));
        }

        [Test]
        public async Task EachIdentityKeepsItsMostRecentValueAcrossSeveralSaves()
        {
            IDeltaStore store = new MemoryDeltaStore();
            var setForA = new List<string>();
            var setForB = new List<string>();

            for (int round = 1; round <= 3; round++)
            {
                IDelta deltaA = store.CreateDelta(IdentityA, $"A operation {round}");
                IDelta deltaB = store.CreateDelta(IdentityB, $"B operation {round}");
                await store.SaveDeltasAsync(new List<IDelta>() { deltaA, deltaB }, default);

                Assert.IsFalse(string.IsNullOrEmpty(deltaA.Index), "the save should give delta A an index");
                Assert.IsFalse(string.IsNullOrEmpty(deltaB.Index), "the save should give delta B an index");
                Assert.AreNotEqual(deltaA.Index, deltaB.Index);

                await store.SetLastProcessedDeltaAsync(deltaA.Index, IdentityA, default);
                await store.SetLastProcessedDeltaAsync(deltaB.Index, IdentityB, default);
                setForA.Add(deltaA.Index);
                setForB.Add(deltaB.Index);

                string actualA = await store.GetLastProcessedDeltaAsync(IdentityA, default);
                string actualB = await store.GetLastProcessedDeltaAsync(IdentityB, default);

                Assert.AreEqual(deltaA.Index, actualA, $"round {round}: identity A");
                Assert.AreEqual(deltaB.Index, actualB, $"round {round}: identity B");
                CollectionAssert.DoesNotContain(setForB, actualA, $"round {round}: identity A returned a value of identity B");
                CollectionAssert.DoesNotContain(setForA, actualB, $"round {round}: identity B returned a value of identity A");
                CollectionAssert.DoesNotContain(setForA.GetRange(0, setForA.Count - 1), actualA, $"round {round}: identity A returned an older value");
                CollectionAssert.DoesNotContain(setForB.GetRange(0, setForB.Count - 1), actualB, $"round {round}: identity B returned an older value");
            }

            // A save that comes after the last update does not move either value.
            await store.SaveDeltasAsync(new List<IDelta>()
            {
                store.CreateDelta(IdentityA, "A operation after"),
                store.CreateDelta(IdentityB, "B operation after")
            }, default);

            Assert.AreEqual(setForA[setForA.Count - 1], await store.GetLastProcessedDeltaAsync(IdentityA, default));
            Assert.AreEqual(setForB[setForB.Count - 1], await store.GetLastProcessedDeltaAsync(IdentityB, default));
        }

        [Test]
        public async Task IdentityNeverSetDoesNotReturnAnotherIdentitysValue()
        {
            IDeltaStore store = new MemoryDeltaStore();
            await store.SetLastProcessedDeltaAsync("A1", IdentityA, default);
            await store.SetLastProcessedDeltaAsync("B1", IdentityB, default);
            await store.SetLastProcessedDeltaAsync("A2", IdentityA, default);

            string actual = await store.GetLastProcessedDeltaAsync("identityC", default);

            CollectionAssert.DoesNotContain(new[] { "A1", "A2", "B1" }, actual);
        }

        [Test]
        public async Task PushingForOneIdentityDoesNotChangeAnyLastProcessedDelta()
        {
            IDeltaStore store = new MemoryDeltaStore();
            await store.SetLastProcessedDeltaAsync("A1", IdentityA, default);
            await store.SetLastProcessedDeltaAsync("B1", IdentityB, default);

            await store.SetLastPushedDeltaAsync("P1", IdentityA, default);
            await store.SetLastPushedDeltaAsync("P2", "identityC", default);

            Assert.AreEqual("A1", await store.GetLastProcessedDeltaAsync(IdentityA, default));
            Assert.AreEqual("B1", await store.GetLastProcessedDeltaAsync(IdentityB, default));
            CollectionAssert.DoesNotContain(new[] { "A1", "B1", "P1", "P2" }, await store.GetLastProcessedDeltaAsync("identityC", default));
        }
    }
}
