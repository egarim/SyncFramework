using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessDeltaBaseEventArgs : SyncEventBase
    {
        public IDelta Delta { get; set; }

        public ProcessDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}