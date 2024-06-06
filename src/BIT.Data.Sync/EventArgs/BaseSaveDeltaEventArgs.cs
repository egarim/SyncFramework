using BIT.Data.Sync.Server;
using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EventArgs
{
    public class BaseSaveDeltaEventArgs : BaseSyncEvent
    {
        public IDelta Delta { get; set; }

        public BaseSaveDeltaEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}