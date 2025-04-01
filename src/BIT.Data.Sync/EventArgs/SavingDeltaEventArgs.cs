using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SavingDeltaEventArgs : BaseDeltaEventArgs
    {
        public SavingDeltaEventArgs(IDelta delta) : base(delta)
        {
            CustomHandled = false;
        }

        public bool CustomHandled { get; set; }
    }
}