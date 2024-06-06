using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavedDeltaEventArgs : SavedDeltaEventArgs
    {

        public NodeSavedDeltaEventArgs(IDelta delta, ISyncServerNode node, BaseSaveDeltaEventArgs saveDeltaBaseEventArgs) : base(delta)
        {
            Node = node;
            this.SaveDeltaArgs = saveDeltaBaseEventArgs;
        }
        BaseSaveDeltaEventArgs SaveDeltaArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}