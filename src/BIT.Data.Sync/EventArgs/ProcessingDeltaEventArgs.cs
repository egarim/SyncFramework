using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessingDeltaEventArgs : SaveDeltaBaseEventArgs
    {
        public ProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            Handled = false;
        }

        public bool Handled { get; set; }
    }
}