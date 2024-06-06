using System;

namespace BIT.Data.Sync.EventArgs
{
    public class ProcessingDeltaEventArgs : BaseSaveDeltaEventArgs
    {
        public ProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            Handled = false;
        }

        public bool Handled { get; set; }
    }
}