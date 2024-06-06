using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavedDeltaEventArgs : SaveDeltaBaseEventArgs
    {

        public NodeSavedDeltaEventArgs(IDelta delta, ISyncServerNode node, SaveDeltaBaseEventArgs saveDeltaBaseEventArgs) : base(delta)
        {
            Node = node;
            this.saveDeltaBaseEventArgs = saveDeltaBaseEventArgs;
        }
        SaveDeltaBaseEventArgs saveDeltaBaseEventArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}