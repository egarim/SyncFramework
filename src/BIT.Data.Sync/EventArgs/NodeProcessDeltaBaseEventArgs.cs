using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeProcessDeltaBaseEventArgs : ProcessDeltaBaseEventArgs
    {

        public NodeProcessDeltaBaseEventArgs(IDelta delta, ISyncServerNode node, ProcessDeltaBaseEventArgs processingDeltaEventArgs) : base(delta)
        {
            Node = node;
            this.processingDeltaEventArgs = processingDeltaEventArgs;
        }
        public ISyncServerNode Node { get; set; }
        public ProcessDeltaBaseEventArgs processingDeltaEventArgs { get; set; }
    }
}