using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SavingDeltaEventArgs : SaveDeltaBaseEventArgs
    {
        public SavingDeltaEventArgs(IDelta delta) : base(delta)
        {
            Handled = false;
        }

        public bool Handled { get; set; }
    }
}