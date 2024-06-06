using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerSavingDeltaEventArgs : SavingDeltaEventArgs
    {
        public ServerSavingDeltaEventArgs(IDelta delta) : base(delta)
        {

        }
        public ServerSavingDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
    }
}