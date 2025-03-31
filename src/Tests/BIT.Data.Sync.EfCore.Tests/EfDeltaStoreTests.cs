using BIT.Data.Sync.Imp;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BIT.Data.Sync.Tests
{
        public class EfDeltaStoreTests 
    {
      
        [SetUp()]
        public  void Setup()
        {

        }
 

        [Test]
        public async Task SaveDeltasAsync_Test()
        {

        //        ailed: BIT.Data.Sync.Tests.EfDeltaStoreTests.SaveDeltasAsync_Test
        //  System.InvalidOperationException : Unable to resolve service for type 'BIT.Data.Sync.ISequenceService'.This is often because no database provider has been configured for this DbContext.A provider can be configured by overriding the 'DbContext.OnConfiguring' method or by using 'AddDbContext' on the application service provider.If 'AddDbContext' is used, then also ensure that your DbContext type accepts a DbContextOptions<TContext> object in its constructor and passes it to the base constructor for DbContext.
        //  System.InvalidOperationException : Unable to resolve service for type 'BIT.Data.Sync.ISequenceService'.This is often because no database provider has been configured for this DbContext.A provider can be configured by overriding the 'DbContext.OnConfiguring' method or by using 'AddDbContext' on the application service provider.If 'AddDbContext' is used, then also ensure that your DbContext type accepts a DbContextOptions<TContext> object in its constructor and passes it to the base constructor for DbContext.
        //EntityFrameworkCore\BIT.Data.Sync.EfCore\EFDeltaStore.cs(21, 0) : BIT.EfCore.Sync.EfDeltaStore..ctor(DeltaDbContext DeltaDbContext)
        //Tests\BIT.Data.Sync.EfCore.Tests\EfDeltaStoreTests.cs(52, 0) : BIT.Data.Sync.Tests.EfDeltaStoreTests.SaveDeltasAsync_Test()
        // Summary: Passed: 3, Failed: 1, Ignored: 0


            ServiceCollection ServiceCollectionMaster = new ServiceCollection();

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            ServiceCollectionMaster.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);
           



            //ServiceCollectionMaster.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_A.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_B.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_C.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));


            ServiceCollectionMaster.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
          


            var _providerMaster = ServiceCollectionMaster.BuildServiceProvider();
          

            var options = new DbContextOptionsBuilder<DeltaDbContext>()
          .UseInMemoryDatabase(databaseName: nameof(SaveDeltasAsync_Test))
          .Options;

         

            IDeltaStore memoryDeltaStore = new EfDeltaStore(new DeltaDbContext(options));
            //IDeltaStore memoryDeltaStore = new MemoryDeltaStore(new List<IDelta>());

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
        //TODO add test for GetLastProcessedDeltaAsync check the it should never be "" it should be the same as sequence service GetMinValue
    }
}