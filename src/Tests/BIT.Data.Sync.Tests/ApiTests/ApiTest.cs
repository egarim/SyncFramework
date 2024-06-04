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
        
    }
}