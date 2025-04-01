using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessedDeltaEventArgs : BaseSyncEvent
    {

        public NodeProcessedDeltaEventArgs(ISyncServerNode node, ProcessedDeltaEventArgs Args) 
        {
            Node = node;
            this.ProcessedDeltaArgs = Args;
        }
        public ISyncServerNode Node { get; set; }
        public ProcessedDeltaEventArgs ProcessedDeltaArgs { get; set; }
    }
}