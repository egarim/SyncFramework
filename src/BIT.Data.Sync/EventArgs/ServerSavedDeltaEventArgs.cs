using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerSavedDeltaEventArgs : SavedDeltaEventArgs
    {

        public ServerSavedDeltaEventArgs(IDelta delta) : base(delta)
        {
        }
        public ServerSavedDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
    }
}