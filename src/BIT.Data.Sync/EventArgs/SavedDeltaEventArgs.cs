using System;

namespace BIT.Data.Sync.EventArgs
{
    public class SavedDeltaEventArgs : BaseSaveDeltaEventArgs
    {
        public SavedDeltaEventArgs(IDelta delta) : base(delta)
        {

        }


    }
}