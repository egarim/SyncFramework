using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using BIT.Data.Sync.Imp;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BIT.Data.Sync.Tests
{
        public class MemoryDeltaStoreTests 
    {
      
        [SetUp()]
        public  void Setup()
        {

        }
 

        [Test]
        public async Task SaveDeltasAsync_Test()
        {

            IDeltaStore memoryDeltaStore = new MemoryDeltaStore(new List<IDelta>());

            var  DeltaHello=  memoryDeltaStore.CreateDelta("A", "Hello");

            await memoryDeltaStore.SaveDeltasAsync(new List<IDelta>(){ DeltaHello },default);

            Assert.AreEqual(1, await memoryDeltaStore.GetDeltaCountAsync(string.Empty,"A",default));

        }

        [Test]
        public async Task SetAndGetLastProcessedDelta_Test()
        {
            IDeltaStore memoryDeltaStore = new MemoryDeltaStore(new List<IDelta>());

            var DeltaHello = memoryDeltaStore.CreateDelta("A", "Hello");

            await memoryDeltaStore.SetLastProcessedDeltaAsync(DeltaHello.Index,"A",default);

            var actual = await memoryDeltaStore.GetLastProcessedDeltaAsync("A", default);
            Assert.AreEqual(DeltaHello.Index, actual);

        }

        [Test]
        public async Task GetDeltasAsync_Test()
        {
            IDeltaStore memoryDeltaStore = new MemoryDeltaStore(new List<IDelta>());

            var DeltaHello = memoryDeltaStore.CreateDelta("A", "Hello");
            var DeltaWorld = memoryDeltaStore.CreateDelta("A", "World");

            List<IDelta> deltas = new List<IDelta>() { DeltaHello, DeltaWorld };
            await memoryDeltaStore.SaveDeltasAsync(deltas, default);

            IEnumerable<IDelta> DeltasFromStore = await memoryDeltaStore.GetDeltasAsync(string.Empty, default);
           

            Assert.NotNull(DeltasFromStore.FirstOrDefault(d=>d.Index==DeltaHello.Index));
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