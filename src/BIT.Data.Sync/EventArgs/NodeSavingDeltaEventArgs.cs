using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavingDeltaEventArgs : SavedDeltaEventArgs
    {

        public NodeSavingDeltaEventArgs(IDelta delta, ISyncServerNode node, SaveDeltaBaseEventArgs saveDeltaBaseEventArgs) : base(delta)
        {
            Node = node;
            this.saveDeltaBaseEventArgs = saveDeltaBaseEventArgs;
        }
        SaveDeltaBaseEventArgs saveDeltaBaseEventArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}