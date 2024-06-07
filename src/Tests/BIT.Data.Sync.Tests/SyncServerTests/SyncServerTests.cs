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
        public async Task ServerEvents()
        {
            var NodeId="Node1";
            ISyncServer server = new SyncServer();
            server.RegisterNodeFunction =  (node) =>
            {
                string NodeId = node.Options.FirstOrDefault(k => k.Key == "NodeId").Value;
                return new SyncServerNode(new MemoryDeltaStore(), new SimpleDatabaseDeltaProcessor(new List<SimpleDatabaseRecord>(),null), NodeId);
            };
            server.RegisterNodeAsync(new RegisterNodeRequest() { Options = new System.Collections.Generic.List<Option>() { new Option("NodeId", NodeId) } });


            List<IDelta> list = new List<IDelta>();
            byte[] data = Encoding.UTF8.GetBytes("Test");
            list.Add(SerializationHelper.CreateDeltaCore("Test",new SimpleDatabaseModification( OperationType.Add,new SimpleDatabaseRecord())));
            bool SavedDelta = false;
            bool SavingDelta = false;
            bool ProcessingDelta = false;
            bool ProcessedDelta = false;
            ISyncServerWithEvents ServerWithEvents = (ISyncServerWithEvents)server;
            ServerWithEvents.SavingDelta += (sender, e) =>
            {
                SavingDelta= true;
                Debug.WriteLine("SavingDelta");
            };
            ServerWithEvents.SavedDelta += (sender, e) =>
            {
                SavedDelta = true;
                Debug.WriteLine("SavedDelta");
            };
            ServerWithEvents.ProcessingDelta += (sender, e) =>
            {
                ProcessingDelta = true;
                e.Handled = true;
                Debug.WriteLine("ProcessingDelta");
            };
            ServerWithEvents.ProcessedDelta+= (sender, e) =>
            {
                ProcessedDelta = true;
                Debug.WriteLine("ProcessedDelta");
            };


            await server.SaveDeltasAsync(NodeId, list, default);
            await server.ProcessDeltasAsync(NodeId, list, default);

            Assert.IsTrue(SavingDelta);
            Assert.IsTrue(SavedDelta);
            Assert.IsTrue(ProcessingDelta);
            Assert.IsTrue(ProcessedDelta);
        }
      
    

    }

   
}