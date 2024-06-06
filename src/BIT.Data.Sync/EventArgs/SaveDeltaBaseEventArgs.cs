using BIT.Data.Sync.Server;
using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EventArgs
{
    public class SaveDeltaBaseEventArgs : SyncEventBase
    {
        public IDelta Delta { get; set; }

        public SaveDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}