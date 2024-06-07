using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavingDeltaEventArgs : BaseSyncEvent
    {

        public NodeSavingDeltaEventArgs(ISyncServerNode node, SavingDeltaEventArgs Args)
        {
            Node = node;
            this.SavingDeltaArgs = Args;
        }
        public SavingDeltaEventArgs SavingDeltaArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}