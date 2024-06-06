using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SyncServerSavingDeltaEventArgs : SavingDeltaEventArgs
    {
        public SyncServerSavingDeltaEventArgs(IDelta delta) : base(delta)
        {

        }
        public SyncServerSavingDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
    }
}