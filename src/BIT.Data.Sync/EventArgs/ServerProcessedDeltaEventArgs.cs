using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerProcessedDeltaEventArgs : BaseSyncEvent
    {
    
 
        public NodeProcessedDeltaEventArgs NodeProcessedArgs { get ; set ; }

        public ServerProcessedDeltaEventArgs(NodeProcessedDeltaEventArgs args)
        {
         
            this.NodeProcessedArgs = args;
       
        }
     

    }
}