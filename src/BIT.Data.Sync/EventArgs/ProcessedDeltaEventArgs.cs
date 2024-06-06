using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessedDeltaEventArgs : BaseProcessedDeltaEventArgs
    {

        public ProcessedDeltaEventArgs(IDelta delta) : base(delta)
        {
        }
    }
}