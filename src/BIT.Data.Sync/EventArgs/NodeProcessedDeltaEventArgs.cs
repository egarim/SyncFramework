using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessedDeltaEventArgs : BaseSyncEvent
    {

        public NodeProcessedDeltaEventArgs(IServerSyncEndpoint node, ProcessedDeltaEventArgs Args) 
        {
            Node = node;
            this.ProcessedDeltaArgs = Args;
        }
        public IServerSyncEndpoint Node { get; set; }
        public ProcessedDeltaEventArgs ProcessedDeltaArgs { get; set; }
    }
}