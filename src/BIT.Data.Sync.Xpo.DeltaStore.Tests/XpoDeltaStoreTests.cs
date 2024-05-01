using BIT.Data.Sync.Imp;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;

namespace BIT.Data.Sync.Xpo.DeltaStore.Tests
{
    public class Tests
    {
        public class XpoDeltaStoreTests
        {

            [SetUp()]
            public void Setup()
            {

            }


            [Test]
            public async Task SaveDeltasAsync_Test()
            {
                IDeltaStore DeltaStore = GetDeltaStore();

                var DeltaHello = DeltaStore.CreateDelta("A", "Hello");

                await DeltaStore.SaveDeltasAsync(new List<IDelta>() { DeltaHello }, default);

                Assert.AreEqual(1, await DeltaStore.GetDeltaCountAsync(string.Empty, "A", default));

            }

            private static IDeltaStore GetDeltaStore()
            {
                var Dal = new SimpleDataLayer(new InMemoryDataStore());
                XpoSequenceService SequenceService = new XpoSequenceService(new YearSequencePrefixStrategy(), Dal);
                IDeltaStore DeltaStore = new XpoDeltaStore(Dal, SequenceService);
                return DeltaStore;
            }

            [Test]
            public async Task SetAndGetLastProcessedDelta_Test()
            {
                IDeltaStore DeltaStore = GetDeltaStore();

                var DeltaHello = DeltaStore.CreateDelta("A", "Hello");

                await DeltaStore.SetLastProcessedDeltaAsync(DeltaHello.Index, "A", default);

                var actual = await DeltaStore.GetLastProcessedDeltaAsync("A", default);
                Assert.AreEqual(DeltaHello.Index, actual);

            }

            [Test]
            public async Task GetDeltasAsync_Test()
            {
                IDeltaStore memoryDeltaStore = new MemoryDeltaStore(new List<IDelta>());

                var DeltaHello = memoryDeltaStore.CreateDelta("A", "Hello");
                var DeltaWorld = memoryDeltaStore.CreateDelta("A", "World");

                await memoryDeltaStore.SaveDeltasAsync(new List<IDelta>() { DeltaHello, DeltaWorld }, default);

                IEnumerable<IDelta> DeltasFromStore = await memoryDeltaStore.GetDeltasAsync(string.Empty, default);


                Assert.NotNull(DeltasFromStore.FirstOrDefault(d => d.Index == DeltaHello.Index));
                Assert.NotNull(DeltasFromStore.FirstOrDefault(d => d.Index == DeltaWorld.Index));
            }
            [Test]
            public async Task PurgeDeltasAsync_Test()
            {
                MemoryDeltaStore memoryDeltaStore = new MemoryDeltaStore();

                var DeltaHello = memoryDeltaStore.CreateDelta("A", "Hello");

                await memoryDeltaStore.SaveDeltasAsync(new List<IDelta>() { DeltaHello });

                await memoryDeltaStore.PurgeDeltasAsync("");
                Assert.AreEqual(0, await memoryDeltaStore.GetDeltaCountAsync(string.Empty, ""));

            }
        }
    }
}