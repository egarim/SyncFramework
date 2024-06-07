using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessedDeltaEventArgs : BaseDeltaEventArgs
    {
        public bool CustomHandled { get; set; }
        public ProcessedDeltaEventArgs(IDelta delta, bool customHandled) : base(delta)
        {
            CustomHandled = customHandled;  
        }
    }
}