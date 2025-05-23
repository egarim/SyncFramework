using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavedDeltaEventArgs : BaseSyncEvent
    {

        public NodeSavedDeltaEventArgs(IServerSyncEndpoint node, SavedDeltaEventArgs Args) 
        {
            Node = node;
            this.SaveDeltaArgs = Args;
        }
        public SavedDeltaEventArgs SaveDeltaArgs { get; set; }
        public IServerSyncEndpoint Node { get; set; }
    }
}