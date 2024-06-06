using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessingDeltaEventArgs : ProcessingDeltaEventArgs
    {
        public NodeProcessingDeltaEventArgs(IDelta delta, ISyncServerNode node, ProcessingDeltaEventArgs processingDeltaEventArgs) : base(delta)
        {
            Node = node;
            this.ProcessingDeltaArgs = processingDeltaEventArgs;
        }
        public ISyncServerNode Node { get; set; }
        public ProcessingDeltaEventArgs ProcessingDeltaArgs { get; set; }
    }
}