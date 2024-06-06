using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessedDeltaEventArgs : ProcessedDeltaEventArgs
    {

        public NodeProcessedDeltaEventArgs(IDelta delta, ISyncServerNode node, ProcessedDeltaEventArgs processingDeltaEventArgs) : base(delta)
        {
            Node = node;
            this.ProcessedDeltaArgs = processingDeltaEventArgs;
        }
        public ISyncServerNode Node { get; set; }
        public ProcessedDeltaEventArgs ProcessedDeltaArgs { get; set; }
    }
}