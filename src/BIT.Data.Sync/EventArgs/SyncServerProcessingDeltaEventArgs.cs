using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SyncServerProcessingDeltaEventArgs : ProcessingDeltaEventArgs
    {
        public SyncServerProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            Handled = false;
        }
        public SyncServerProcessingDeltaEventArgs(IDelta delta, ISyncServerNode node) : this(delta)
        {
            Node = node;

        }
        public ISyncServerNode Node { get; set; }


    }
}