using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessingDeltaEventArgs : BaseSyncEvent
    {
        public NodeProcessingDeltaEventArgs(ISyncServerNode node, ProcessingDeltaEventArgs Args) 
        {
            Node = node;
            this.ProcessingDeltaArgs = Args;
        }
        public ISyncServerNode Node { get; set; }
        public ProcessingDeltaEventArgs ProcessingDeltaArgs { get; set; }
    }
}