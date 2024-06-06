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
using System.Collections.Generic;
namespace BIT.Data.Sync.Tests.SyncServerTests
{
    public class SyncServerTests : MultiServerBaseTest
    {
   

        [SetUp()]
        public override void Setup()
        {
            base.Setup();
        }
        [Test]
        public async Task SaveEventsTest()
        {
            var NodeId="Node1";
            ISyncServer server = new SyncServer();
            server.RegisterNodeFunction =  (node) =>
            {
                string NodeId = node.Options.FirstOrDefault(k => k.Key == "NodeId").Value;
                return new SyncServerNode(new MemoryDeltaStore(), null, NodeId);
            };
            server.RegisterNodeAsync(new RegisterNodeRequest() { Options = new System.Collections.Generic.List<Option>() { new Option("NodeId", NodeId) } });


            List<IDelta> list = new List<IDelta>();
            byte[] data = Encoding.UTF8.GetBytes("Test");
            list.Add(new Delta("Test", "Test", data));
            bool SavedDelta = false;
            bool SavingDelta = false;
            ((ISyncServerWithEvents)server).SavingDelta += (sender, e) =>
            {
                SavingDelta= true;
                Debug.WriteLine("SavingDelta");
            };
            ((ISyncServerWithEvents)server).SavedDelta += (sender, e) =>
            {
                SavedDelta = true;
                Debug.WriteLine("SavedDelta");
            };
            await server.SaveDeltasAsync(NodeId, list, default);

            Assert.IsTrue(SavingDelta);
            Assert.IsTrue(SavedDelta);
        }
      
    

    }

   
}