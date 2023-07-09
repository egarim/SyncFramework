using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Components;

namespace SyncFramework.Playground.Shared
{
    public partial class SyncServerComponent
    {
        [Parameter]
        public IDeltaStore deltaStore { get; set; }
        [Parameter]
        public string NodeId { get; set; }
        protected override void OnInitialized()
        {
            SyncServerNode syncServerNode = new SyncServerNode(deltaStore, null, NodeId);
            var Server = new SyncServer(syncServerNode);
            this.HttpClient = new HttpClient(new FakeHandler(Server));
            base.OnInitialized();
        }
        public HttpClient HttpClient { get; set; }
    }
}
