using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class NodeSavedDeltaEventArgs : BaseSyncEvent
    {

        public NodeSavedDeltaEventArgs(ISyncServerNode node, SavedDeltaEventArgs Args) 
        {
            Node = node;
            this.SaveDeltaArgs = Args;
        }
        public SavedDeltaEventArgs SaveDeltaArgs { get; set; }
        public ISyncServerNode Node { get; set; }
    }
}