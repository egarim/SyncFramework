using BIT.Data.Sync.Server;
using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ServerSavedDeltaEventArgs : BaseSyncEvent
    {


    
        public NodeSavedDeltaEventArgs NodeSavedArgs { get; set; }

        public ServerSavedDeltaEventArgs(NodeSavedDeltaEventArgs args)
        {

            this.NodeSavedArgs = args;
          





        }
    
    }
}