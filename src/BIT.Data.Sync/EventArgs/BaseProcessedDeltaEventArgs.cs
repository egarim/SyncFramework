using System;

namespace BIT.Data.Sync.EventArgs
{
    public class BaseProcessedDeltaEventArgs : BaseSyncEvent
    {
        public IDelta Delta { get; set; }

        public BaseProcessedDeltaEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}