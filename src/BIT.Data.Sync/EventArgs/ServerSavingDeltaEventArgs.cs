using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerSavingDeltaEventArgs : BaseSyncEvent
    {
        public ServerSavingDeltaEventArgs(NodeSavingDeltaEventArgs args)
        {
          
            this.NodeSavingArgs = args;
        }
 
        public NodeSavingDeltaEventArgs NodeSavingArgs { get; set; }
    
    }
}