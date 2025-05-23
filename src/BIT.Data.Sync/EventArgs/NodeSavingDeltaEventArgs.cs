using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavingDeltaEventArgs : BaseSyncEvent
    {

        public NodeSavingDeltaEventArgs(IServerSyncEndpoint node, SavingDeltaEventArgs Args)
        {
            Node = node;
            this.SavingDeltaArgs = Args;
        }
        public SavingDeltaEventArgs SavingDeltaArgs { get; set; }
        public IServerSyncEndpoint Node { get; set; }
    }
}