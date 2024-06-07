using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SavedDeltaEventArgs : BaseDeltaEventArgs
    {
        public bool CustomHandled { get; set; }
        public SavedDeltaEventArgs(IDelta delta, bool customHandled) : base(delta)
        {
            CustomHandled = customHandled;
        }


    }
}