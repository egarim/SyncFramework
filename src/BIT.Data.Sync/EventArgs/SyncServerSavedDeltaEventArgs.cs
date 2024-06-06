using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SyncServerSavedDeltaEventArgs : SavedDeltaEventArgs
    {

        public SyncServerSavedDeltaEventArgs(IDelta delta) : base(delta)
        {
        }
        public SyncServerSavedDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
    }
}