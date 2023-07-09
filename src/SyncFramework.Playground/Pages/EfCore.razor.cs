using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SyncFramework.Playground.Shared;
using System.Net.Http;
using System.Security.Principal;
namespace SyncFramework.Playground.Pages
{
    public partial class EfCore
    {

        [Inject]
        public DeltaGeneratorBase[] deltaGeneratorBases { get; set; }
        public IDeltaStore deltaStore { get; set; }
        public string NodeId { get; set; }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            deltaStore = new MemoryDeltaStore();
            NodeId = "MainServer";
        }
        SyncServerComponent serverComponent;
        private void AddClientNode()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSyncFrameworkForSQLite($"Data Source=test.db", this.serverComponent.HttpClient, this.serverComponent.NodeId, "Node A", deltaGeneratorBases);

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            serviceCollection.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);

            serviceCollection.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            var _providerNode_A = serviceCollection.BuildServiceProvider();
        }
    }
}
