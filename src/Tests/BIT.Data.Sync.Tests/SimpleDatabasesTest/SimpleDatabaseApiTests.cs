using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
namespace BIT.Data.Sync.Tests.SimpleDatabasesTest
{
    public class SimpleDatabaseApiTests : MultiServerBaseTest
    {

        [SetUp()]
        public override void Setup()
        {
            base.Setup();
        }
        [Test]
        public async Task SyncMultipleClients_CRUD_Test_Network_Test()
        {
        
                //0 - Get the network client connected to the API controller exposed by the test infrastructure
                var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
                ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, "MemoryDeltaStore1");

                //1 - Create the master database and its delta processor
                SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);
                SimpleDatabaseDeltaProcessor Master_DeltaProcessor = new SimpleDatabaseDeltaProcessor(
                    Master.Data,
                    new MemorySequenceService(new YearSequencePrefixStrategy()));
                //Master.DeltaProcessor = Master_DeltaProcessor;

                //2- Create data and save it on the master
                SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
                SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

                await Master.Add(Hello);
                await Master.Add(World);

                //3- Creating database A and its delta processor
                SimpleDatabase A_Database = new SimpleDatabase("A", syncFrameworkClient);
                SimpleDatabaseDeltaProcessor A_DeltaProcessor = new SimpleDatabaseDeltaProcessor(
                    A_Database.Data,
                    new MemorySequenceService(new YearSequencePrefixStrategy()));
                //A_Database.DeltaProcessor = A_DeltaProcessor;

                //4- Create data and save it on database A
                SimpleDatabaseRecord Hola = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hola" };
                SimpleDatabaseRecord Mundo = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Mundo" };

                await A_Database.Add(Hola);
                await A_Database.Add(Mundo);

                //5 - Creating database B and its delta processor
                SimpleDatabase B_Database = new SimpleDatabase("B", syncFrameworkClient);
                SimpleDatabaseDeltaProcessor B_DeltaProcessor = new SimpleDatabaseDeltaProcessor(
                    B_Database.Data,
                    new MemorySequenceService(new YearSequencePrefixStrategy()));
                //B_Database.DeltaProcessor = B_DeltaProcessor;

                //6 - Create data and save it on database B
                SimpleDatabaseRecord Privet = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Privet" };
                SimpleDatabaseRecord Mir = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "mir" };
                await B_Database.Add(Privet);
                await B_Database.Add(Mir);

                //7 - Push deltas to the server
                await Master.PushAsync();
                await A_Database.PushAsync();
                await B_Database.PushAsync();

                //8 - Pull deltas from server
                await Master.PullAsync();
                await A_Database.PullAsync();
                await B_Database.PullAsync();

                //9 - Write in the console the current state of each database
                Debug.WriteLine("Data in master");
                Master.Data.ForEach(r => Debug.WriteLine(r.ToString()));
                Debug.WriteLine("Data in master Last Processed Delta Index:" +
                               await Master.DeltaStore.GetLastProcessedDeltaAsync(Master.Identity, default));

                Debug.WriteLine("Data in A_Database");
                A_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
                string A_LastIndexProcessed = await A_Database.DeltaStore.GetLastProcessedDeltaAsync(A_Database.Identity, default);
                Debug.WriteLine("Data in A_Database Last Processed Delta Index:" + A_LastIndexProcessed);

                Debug.WriteLine("Data in B_Database");
                B_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
                string B_LastIndexProcessed = await B_Database.DeltaStore.GetLastProcessedDeltaAsync(B_Database.Identity, default);
                Debug.WriteLine("Data in B_Database Last Processed Delta Index:" + B_LastIndexProcessed);

                // Verify all databases have all 6 records after first sync
                Assert.AreEqual(6, Master.Data.Count, "Master should have 6 records after initial sync");
                Assert.AreEqual(6, A_Database.Data.Count, "A_Database should have 6 records after initial sync");
                Assert.AreEqual(6, B_Database.Data.Count, "B_Database should have 6 records after initial sync");

                //10 - Delete and update records in the master database
                Master.Delete(Hola);
                Mundo.Text = "HOLA MUNDO";
                Master.Update(Mundo);

                await Master.PushAsync();
                await A_Database.PullAsync();
                await B_Database.PullAsync();

                Debug.WriteLine($"{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}{System.Environment.NewLine}");

                //11 - Write in the console the current state of each database
                Debug.WriteLine("Data in master");
                Master.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));
                Debug.WriteLine("Data in A_Database");
                A_Database.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));
                Debug.WriteLine("Data in B_Database");
                B_Database.Data.OrderBy(x => x.Key).ToList().ForEach(r => Debug.WriteLine(r.ToString()));

                // Final assertions - after delete we should have 5 records in each database
                Assert.AreEqual(5, Master.Data.Count, "Master should have 5 records after deletion");
                Assert.AreEqual(5, A_Database.Data.Count, "A_Database should have 5 records after deletion");
                Assert.AreEqual(5, B_Database.Data.Count, "B_Database should have 5 records after deletion");

                // Verify the text was updated correctly
                var updatedMundoInA = A_Database.Data.FirstOrDefault(r => r.Key == Mundo.Key);
                Assert.IsNotNull(updatedMundoInA, "Updated record should exist in database A");
                Assert.AreEqual("HOLA MUNDO", updatedMundoInA.Text, "Text should be updated in database A");

                var updatedMundoInB = B_Database.Data.FirstOrDefault(r => r.Key == Mundo.Key);
                Assert.IsNotNull(updatedMundoInB, "Updated record should exist in database B");
                Assert.AreEqual("HOLA MUNDO", updatedMundoInB.Text, "Text should be updated in database B");
            

        }

    }
}