using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SyncServerProcessDeltaBaseEventArgs : ProcessDeltaBaseEventArgs
    {
        public SyncServerProcessDeltaBaseEventArgs(IDelta delta) : base(delta)
        {

        }
        public ISyncServerNode Node { get; set; }
        public SyncServerProcessDeltaBaseEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
    }
}