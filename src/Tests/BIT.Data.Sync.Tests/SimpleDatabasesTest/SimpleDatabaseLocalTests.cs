using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests.SimpleDatabasesTest
{
    public class SimpleDatabaseLocalTests
    {

        [SetUp()]
        public void Setup()
        {
            
        }
        [Test]
        public async Task SyncMultipleClients_CRUD()
        {

            //1 - Delta store for master database
            MemoryDeltaStore MasterDeltaStore = new MemoryDeltaStore();

            //2 - Create the master database
            SimpleDatabase Master = new SimpleDatabase(MasterDeltaStore, "Master");

            //3 - Create a processor to allow the master to process other nodes deltas
            SimpleDatabaseDeltaProcessor Master_DeltaProcessor = new SimpleDatabaseDeltaProcessor(Master.Data);

            //4 - Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);

            //5 - Creating a delta store for database A
            MemoryDeltaStore A_DeltaStore = new MemoryDeltaStore();

            //6 - Creating database A
            SimpleDatabase A_Database = new SimpleDatabase(A_DeltaStore, "A");
            SimpleDatabaseDeltaProcessor A_DeltaProcessor = new SimpleDatabaseDeltaProcessor(A_Database.Data);

            //7 - Create data and save it on database A
            SimpleDatabaseRecord Hola = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hola" };
            SimpleDatabaseRecord Mundo = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Mundo" };

            await A_Database.Add(Hola);
            await A_Database.Add(Mundo);

            //8 - Creating a delta store for database B
            MemoryDeltaStore B_DeltaStore = new MemoryDeltaStore();

            //9 - Creating database B
            SimpleDatabase B_Database = new SimpleDatabase(B_DeltaStore, "B");
            SimpleDatabaseDeltaProcessor B_DeltaProcessor = new SimpleDatabaseDeltaProcessor(B_Database.Data);

            //10 - Create data and save it on database B
            SimpleDatabaseRecord Privet = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Privet" };
            SimpleDatabaseRecord Mir = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "mir" };
            await B_Database.Add(Privet);
            await B_Database.Add(Mir);


            //11 - Get deltas from all databases

            var DeltasFromDatabaseA = await A_Database.DeltaStore.GetDeltasAsync(Guid.Empty, default);
            var DeltasFromDatabaseB = await B_Database.DeltaStore.GetDeltasAsync(Guid.Empty, default);
            var DeltasFromMaster = await Master.DeltaStore.GetDeltasAsync(Guid.Empty, default);

            //12 - Process deltas in the master and save the index of last delta processed
            await Master_DeltaProcessor.ProcessDeltasAsync(DeltasFromDatabaseA, default);
            await Master.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromDatabaseA.Max(d => d.Index),nameof(Master), default);
            await Master_DeltaProcessor.ProcessDeltasAsync(DeltasFromDatabaseB, default);
            await Master.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromDatabaseB.Max(d => d.Index), nameof(Master), default);

            //13 - Process deltas in database A and save the index of last delta processed
            await A_DeltaProcessor.ProcessDeltasAsync(DeltasFromDatabaseB, default);
            await A_Database.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromDatabaseB.Max(d => d.Index), nameof(A_Database), default);
            await A_DeltaProcessor.ProcessDeltasAsync(DeltasFromMaster, default);
            await A_Database.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromMaster.Max(d => d.Index), nameof(A_Database), default);

            //14 - Process deltas in database B and save the index of last delta processed
            await B_DeltaProcessor.ProcessDeltasAsync(DeltasFromDatabaseA, default);
            await B_Database.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromDatabaseA.Max(d => d.Index), nameof(B_Database), default);
            await B_DeltaProcessor.ProcessDeltasAsync(DeltasFromMaster, default);
            await B_Database.DeltaStore.SetLastProcessedDeltaAsync(DeltasFromMaster.Max(d => d.Index), nameof(B_Database), default);

            //15 - Write in the console the current state of each database
            Debug.WriteLine("Data in master");
            Master.Data.ForEach(r => Debug.WriteLine(r.ToString()));
            Debug.WriteLine("Data in master Last Processed Delta Index:" + await Master.DeltaStore.GetLastProcessedDeltaAsync(nameof(Master), default));

            Debug.WriteLine("Data in A_Database");
            A_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
            Guid A_LastIndexProccesded = await A_Database.DeltaStore.GetLastProcessedDeltaAsync(nameof(A_Database), default);
            Debug.WriteLine("Data in A_Database Last Processed Delta Index:" + A_LastIndexProccesded);

            Debug.WriteLine("Data in B_Database");
            B_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
            Guid B_LastIndexProccesded = await B_Database.DeltaStore.GetLastProcessedDeltaAsync(nameof(B_Database), default);
            Debug.WriteLine("Data in B_Database Last Processed Delta Index:" + B_LastIndexProccesded);


            //16 - Delete and update records in the master database
            Master.Delete(Hola);
            Mundo.Text = "HOLA MUNDO";
            Master.Update(Mundo);

            //17 - Get deltas from the master and process them on the other nodes 
            await A_DeltaProcessor.ProcessDeltasAsync(await Master.DeltaStore.GetDeltasAsync(A_LastIndexProccesded, default), default);
            await B_DeltaProcessor.ProcessDeltasAsync(await Master.DeltaStore.GetDeltasAsync(B_LastIndexProccesded, default), default);


            Debug.WriteLine($"{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}");


            //18 - Write in the console the current state of each database
            Debug.WriteLine("Data in master");
            Master.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));
            Debug.WriteLine("Data in A_Database");
            A_Database.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));
            Debug.WriteLine("Data in B_Database");
            B_Database.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));

            Debug.WriteLine("");



            Assert.AreEqual(5, Master.Data.Count);
            Assert.AreEqual(5, A_Database.Data.Count);
            Assert.AreEqual(5, B_Database.Data.Count);
        }
    }
}