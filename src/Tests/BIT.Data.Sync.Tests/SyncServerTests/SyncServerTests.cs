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
            var NodeId = "Node1";
            ISyncServer server = GetSyncServer(NodeId);

            List<IDelta> list = GetDeltas();

            bool SavedDelta = false;
            bool SavingDelta = false;
            bool ProcessingDelta = false;
            bool ProcessedDelta = false;

            ISyncServerWithEvents ServerWithEvents = (ISyncServerWithEvents)server;
            ServerWithEvents.ServerSavingDelta += (sender, e) =>
            {
                SavingDelta = true;
                Debug.WriteLine("SavingDelta");
            };
            ServerWithEvents.ServerSavedDelta += (sender, e) =>
            {
                SavedDelta = true;
                Debug.WriteLine("SavedDelta");
            };
            ServerWithEvents.ServerProcessingDelta += (sender, e) =>
            {

                ProcessingDelta = true;
                Debug.WriteLine("ProcessingDelta");
            };
            ServerWithEvents.ServerProcessedDelta += (sender, e) =>
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
        [Test]
        public async Task CancelingDeltaSavingEvent()
        {
            var NodeId = "Node1";
            ISyncServer server = GetSyncServer(NodeId);

            List<IDelta> list = GetDeltas();


            bool CustomHandled = false;


            ISyncServerWithEvents ServerWithEvents = (ISyncServerWithEvents)server;
            ServerWithEvents.ServerSavingDelta += (sender, e) =>
            {
                
                e.NodeSavingArgs.SavingDeltaArgs.CustomHandled = true;
                Debug.WriteLine("SavingDelta");
            };
            ServerWithEvents.ServerSavedDelta += (sender, e) =>
            {
                CustomHandled=e.NodeSavedArgs.SaveDeltaArgs.CustomHandled;  


                Debug.WriteLine("SavedDelta");
            };


            await server.SaveDeltasAsync(NodeId, list, default);
        

            Assert.IsTrue(CustomHandled);
     
        }
        [Test]
        public async Task CustomHandlingServerSavingDelta()
        {
            var NodeId = "Node1";
            ISyncServer server = GetSyncServer(NodeId);

            List<IDelta> list = GetDeltas();


            bool CustomHandled = false;


            ISyncServerWithEvents ServerWithEvents = (ISyncServerWithEvents)server;
            ServerWithEvents.ServerSavingDelta += (sender, e) =>
            {
              
                e.NodeSavingArgs.SavingDeltaArgs.CustomHandled = true;
                Debug.WriteLine("SavingDelta");
            };
            ServerWithEvents.ServerSavedDelta += (sender, e) =>
            {
                CustomHandled=e.NodeSavedArgs.SaveDeltaArgs.CustomHandled;


                Debug.WriteLine("SavedDelta");
            };



            await server.SaveDeltasAsync(NodeId, list, default);


            Assert.IsTrue(CustomHandled);

        }
        [Test]
        public async Task CustomHandlingServerProcessingDelta()
        {
            var NodeId = "Node1";
            ISyncServer server = GetSyncServer(NodeId);

            List<IDelta> list = GetDeltas();


            bool CustomHandled = false;


            ISyncServerWithEvents ServerWithEvents = (ISyncServerWithEvents)server;
            ServerWithEvents.ServerProcessingDelta += (sender, e) =>
            {
              
                e.NodeProcessingArgs.ProcessingDeltaArgs.CustomHandled = true;
                Debug.WriteLine(nameof(ServerWithEvents.ServerProcessingDelta));
            };
            ServerWithEvents.ServerProcessedDelta += (sender, e) =>
            {
                CustomHandled = e.NodeProcessedArgs.ProcessedDeltaArgs.CustomHandled;
                Debug.WriteLine(nameof(ServerWithEvents.ServerProcessedDelta));

            };



            await server.ProcessDeltasAsync(NodeId, list, default);


            Assert.IsTrue(CustomHandled);

        }
        private static List<IDelta> GetDeltas()
        {
            List<IDelta> list = new List<IDelta>();
            byte[] data = Encoding.UTF8.GetBytes("Test");
            list.Add(SerializationHelper.CreateDeltaCore("Test", new SimpleDatabaseModification(OperationType.Add, new SimpleDatabaseRecord())));
            return list;
        }

        private static ISyncServer GetSyncServer(string NodeId)
        {
            ISyncServer server = new SyncServer();

            server.RegisterNodeFunction = (node) =>
            {
                string ServerNodeId = node.Options.FirstOrDefault(k => k.Key == "NodeId").Value;
                return new SyncServerNode(new MemoryDeltaStore(), new SimpleDatabaseDeltaProcessor(new List<SimpleDatabaseRecord>(), null), ServerNodeId);
            };
            server.RegisterNodeAsync(new RegisterNodeRequest() { Options = new System.Collections.Generic.List<Option>() { new Option("NodeId", NodeId) } });
            return server;
        }


    }

   
}