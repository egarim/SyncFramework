
using System;

namespace BIT.Data.Sync
{
    public class SaveDeltaBaseEventArgs : EventArgs
    {
        public IDelta Delta { get; set; }

        public SaveDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
    public class SavingDeltaEventArgs: SaveDeltaBaseEventArgs
    {
        public SavingDeltaEventArgs(IDelta delta) : base(delta)
        {
            this.Handled = false;
        }

        public bool Handled { get; set; }
    }
    public class ProcessingDeltaEventArgs : SaveDeltaBaseEventArgs
    {
        public ProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            this.Handled = false;
        }

        public bool Handled { get; set; }
    }

    public class ProcessDeltaBaseEventArgs : EventArgs
    {
        public IDelta Delta { get; set; }

        public ProcessDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}