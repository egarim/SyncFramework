using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Tests.Infrastructure;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BIT.Data.Sync.Server;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using BIT.Data.Sync.Tests.Startups;
using System.Threading;
namespace BIT.Data.Sync.Tests.SimpleDatabasesTest
{
    public class ApiTest : MultiServerBaseTest
    {

        [SetUp()]
        public override void Setup()
        {
            base.Setup();
        }
        [Test]
        public async Task RegisterNodeInServer()
        {
            //0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");

            string NodeId = "Custom";

            // Create an instance of RegisterNodeRequest and populate it
            var registerNodeRequest = new RegisterNodeRequest();
            registerNodeRequest.Options.Add(new Option("NodeId", NodeId));
            registerNodeRequest.Options.Add(new Option("key2", "value2"));

            // Serialize the request object to JSON
            string json = JsonSerializer.Serialize(registerNodeRequest);

            // Create the content to send in the POST request
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send the POST request
            HttpResponseMessage response = await httpclient.PostAsync("/Sync/RegisterNode", content);

            // Ensure the request was successful
            response.EnsureSuccessStatusCode();

            // Read and display the response
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);

            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            //1 - Create the master database
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);

            //2- Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);

            await Master.PushAsync();
        }
        [Test]
        public async Task PushResult()
        {
            //0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");

            string NodeId =TestStartup.STR_MemoryDeltaStore1;


            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            //1 - Create the master database
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);

            //2- Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);
            var Result = await Master.PushAsync();
            Assert.IsTrue(Result.Success);


        }
        [Test]
        public async Task PullResult()
        {
            //0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");

            string NodeId = TestStartup.STR_MemoryDeltaStore1;


            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            //1 - Create the master database
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);

            //2- Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);
            var Result = await Master.PullAsync();
            Assert.IsTrue(Result.Success);


        }
        [Test]
        public async Task WrongUrlTest_ForFetch()
        {
            // Arrange: Create an HttpClient with an invalid BaseAddress.
            var httpclient = new HttpClient();

            httpclient.BaseAddress = new Uri("http://localhost:12345/invalid");
            httpclient.Timeout = TimeSpan.FromSeconds(3);
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // Act & Assert: Call FetchAsync directly.
            try
            {
                var fetchResponse = await syncFrameworkClient.FetchAsync(string.Empty, NodeId,default);
                // If no exception was thrown, the response should indicate failure.
                Assert.IsFalse(fetchResponse.Success, "Fetch operation should not succeed with an invalid URL.");
            }
            catch (HttpRequestException ex)
            {
                // Expected exception due to wrong URL.
                Assert.Pass("FetchAsync threw the expected HttpRequestException: " + ex.Message);
            }
        }

        [Test]
        public async Task WrongUrlTest_ForPull()
        {
            // Arrange: Create an HttpClient with an invalid BaseAddress.
            var httpclient = new HttpClient();
            httpclient.BaseAddress = new Uri("http://localhost:12345/invalid");
            httpclient.Timeout = TimeSpan.FromSeconds(1);
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // Create a database instance using the misconfigured client.
            SimpleDatabase db = new SimpleDatabase("Master", syncFrameworkClient);

            // Act & Assert: Call PullAsync.
            try
            {
                var result = await db.PullAsync();
                Assert.IsFalse(result.Success, "Pull operation should not succeed with an invalid URL.");
            }
            catch (HttpRequestException ex)
            {
                // Expected exception due to wrong URL.
                Assert.Pass("PullAsync threw the expected HttpRequestException: " + ex.Message);
            }
        }

        [Test]
        public async Task WrongUrlTest_OverrideBaseAddress()
        {
            // Arrange: Create a client via the factory and override its BaseAddress.
            var httpclient = new HttpClient();//this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost:12345/invalid");
            httpclient.Timeout = TimeSpan.FromSeconds(3);
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // Create a sample database and add a record to push.
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);
            SimpleDatabaseRecord record = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Test" };
            await Master.Add(record);

            // Act & Assert: Expect failure from a push with the wrong URL.
            try
            {
                var result = await Master.PushAsync();
                Assert.IsFalse(result.Success, "Push operation should not succeed with an invalid URL.");
            }
            catch (Exception ex)
            {
                Assert.Pass("PushAsync threw the expected HttpRequestException: " + ex.Message);
            }
        }

        [Test]
        public async Task PushResultWhenNothingToPush()
        {
            // 0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // 1 - Create a database with no data to push
            SimpleDatabase emptyDatabase = new SimpleDatabase("EmptyPush", syncFrameworkClient);

            // 2 - Push without adding any records (nothing to push)
            var Result = await emptyDatabase.PushAsync();

            // 3 - Verify the result
            Assert.IsTrue(Result.Success, "Push operation should succeed even when nothing to push");
            Assert.AreEqual("Nothing to send", Result.Message, "Message should indicate nothing to send");
        }

        [Test]
        public async Task PullResultWhenNothingToPull()
        {
            // 0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // 1 - Create a database 
            SimpleDatabase database = new SimpleDatabase("EmptyPull", syncFrameworkClient);

            // 2 - Add data and push it to the server to set a last processed delta
            SimpleDatabaseRecord testRecord = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Test" };
            await database.Add(testRecord);
            await database.PushAsync();

            // 3 - Pull once to process any deltas and set the last processed delta
            var pullrResult= await database.PullAsync();

            // 4 - Pull again when there are no new deltas to process
            var Result = await database.PullAsync();

            // 5 - Verify the result
            Assert.IsTrue(Result.Success, "Pull operation should succeed even when nothing to pull");
            Assert.AreEqual("Nothing to receive", Result.Message, "Message should indicate nothing to receive");
        }

        [Test]
        public async Task NodeNotFoundTest()
        {
            //0 - Get the network client connected to the API controller exposed by the test infrastructure
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");

            string NodeId = "Custom";

           
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            //1 - Create the master database
            SimpleDatabase Master = new SimpleDatabase("Master", syncFrameworkClient);

            //2- Create data and save it on the master
            SimpleDatabaseRecord Hello = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "Hello" };
            SimpleDatabaseRecord World = new SimpleDatabaseRecord() { Key = Guid.NewGuid(), Text = "World" };

            await Master.Add(Hello);
            await Master.Add(World);
            var Result=await Master.PushAsync();
            //var Result = await Master.FetchAsync();
        }

        [Test]
        public async Task HandShakeTest()
        {
            // Arrange: Get the network client connected to the API controller
            var httpclient = this.GetTestClientFactory().CreateClient("TestClient");
            httpclient.BaseAddress = new Uri("http://localhost/sync/");
            
            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);
            
            // Act: Perform the HandShake operation
            var handshakeResult = await syncFrameworkClient.HandShake(CancellationToken.None);
            
            // Assert: Verify the handshake response
            Assert.IsNotNull(handshakeResult, "HandShake response should not be null");
            Assert.IsTrue(handshakeResult.Success, "HandShake operation should succeed");
            Assert.IsNotNull(handshakeResult.Deltas, "Deltas collection should be initialized");
            //Assert.AreEqual(NodeId, handshakeResult.ClientNodeId, "Client node ID should match the provided ID");
        }

        [Test]
        public async Task HandShakeTest_WrongUrl()
        {
            // Arrange: Create an HttpClient with an invalid BaseAddress
            var httpclient = new HttpClient();
            httpclient.BaseAddress = new Uri("http://localhost:12345/invalid");
            httpclient.Timeout = TimeSpan.FromSeconds(3);

            string NodeId = TestStartup.STR_MemoryDeltaStore1;
            ISyncFrameworkClient syncFrameworkClient = new SyncFrameworkHttpClient(httpclient, NodeId);

            // Act & Assert: Call HandShake and expect failure
            try
            {
                var handshakeResult = await syncFrameworkClient.HandShake(CancellationToken.None);
                // If no exception was thrown, the response should indicate failure
                Assert.IsFalse(handshakeResult.Success, "HandShake operation should not succeed on an empty request.");
            }
            catch (Exception ex)
            {
                // Expected exception due to wrong URL
                Assert.Pass("HandShake threw the expected exception: " + ex.Message);
            }
        }
    }
}