using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerProcessingDeltaEventArgs : BaseSyncEvent
    {

   
        public ServerProcessingDeltaEventArgs(NodeProcessingDeltaEventArgs args) 
        {
   
            this.NodeProcessingArgs = args;
        }

        public NodeProcessingDeltaEventArgs NodeProcessingArgs { get ; set ; }
    }
}