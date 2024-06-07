using BIT.Data.Sync.Server;
using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EventArgs
{
    public class BaseDeltaEventArgs : BaseSyncEvent
    {
        public IDelta Delta { get; set; }

        public BaseDeltaEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}