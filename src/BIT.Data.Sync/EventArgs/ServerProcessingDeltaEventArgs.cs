using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerProcessingDeltaEventArgs : ProcessingDeltaEventArgs
    {
        public ServerProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            Handled = false;
        }
        public ServerProcessingDeltaEventArgs(IDelta delta, ISyncServerNode node) : this(delta)
        {
            Node = node;

        }
        public ISyncServerNode Node { get; set; }


    }
}