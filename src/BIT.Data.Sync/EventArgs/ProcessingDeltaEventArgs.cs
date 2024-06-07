using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessingDeltaEventArgs : BaseDeltaEventArgs
    {
        public ProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            CustomHandled = false;
        }

        public bool CustomHandled { get; set; }
    }
}