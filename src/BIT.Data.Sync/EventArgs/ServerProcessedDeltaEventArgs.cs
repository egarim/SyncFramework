using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerProcessedDeltaEventArgs : BaseProcessedDeltaEventArgs
    {
        public ServerProcessedDeltaEventArgs(IDelta delta) : base(delta)
        {

        }
        public ISyncServerNode Node { get; set; }
        public ServerProcessedDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }

    }
}