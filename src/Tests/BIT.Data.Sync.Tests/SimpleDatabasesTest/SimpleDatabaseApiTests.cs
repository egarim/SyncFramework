using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

            //1 - Create the master database
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);

            //2- Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);

            //3- Creating database A
            SimpleDatabase A_Database = new SimpleDatabase("A", syncFrameworkClient);
            SimpleDatabaseDeltaProcessor A_DeltaProcessor = new SimpleDatabaseDeltaProcessor(A_Database.Data);

            //4- Create data and save it on database A
            SimpleDatabaseRecord Hola = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hola" };
            SimpleDatabaseRecord Mundo = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Mundo" };

            await A_Database.Add(Hola);
            await A_Database.Add(Mundo);


            //5 - Creating database B
            SimpleDatabase B_Database = new SimpleDatabase("B", syncFrameworkClient);

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
            Debug.WriteLine("Data in master Last Processed Delta Index:" + await Master.DeltaStore.GetLastProcessedDeltaAsync(default));

            Debug.WriteLine("Data in A_Database");
            A_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
            Guid A_LastIndexProccesded = await A_Database.DeltaStore.GetLastProcessedDeltaAsync(default);
            Debug.WriteLine("Data in A_Database Last Processed Delta Index:" + A_LastIndexProccesded);

            Debug.WriteLine("Data in B_Database");
            B_Database.Data.ForEach(r => Debug.WriteLine(r.ToString()));
            Guid B_LastIndexProccesded = await B_Database.DeltaStore.GetLastProcessedDeltaAsync(default);
            Debug.WriteLine("Data in B_Database Last Processed Delta Index:" + B_LastIndexProccesded);


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





            Assert.AreEqual(5, Master.Data.Count);
            Assert.AreEqual(5, A_Database.Data.Count);
            Assert.AreEqual(5, B_Database.Data.Count);
        }
        
    }
}