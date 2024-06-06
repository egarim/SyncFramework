using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavingDeltaEventArgs : SavingDeltaEventArgs
    {

        public NodeSavingDeltaEventArgs(IDelta delta, ISyncServerNode node, SavingDeltaEventArgs savingDeltaArgs) : base(delta)
        {
            Node = node;
            this.SavingDeltaArgs = savingDeltaArgs;
        }
        public SavingDeltaEventArgs SavingDeltaArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}