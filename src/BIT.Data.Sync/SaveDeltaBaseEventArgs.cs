
using System;
using System.Collections.Generic;

namespace BIT.Data.Sync
{
    public abstract class SyncEventBase: EventArgs
    {
        
        protected SyncEventBase() {

            Data=new Dictionary<string, object>();
        }
        public Dictionary<string,Object> Data { get; set; }
    }
    public class SaveDeltaBaseEventArgs : SyncEventBase
    {
        public IDelta Delta { get; set; }

        public SaveDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
    public class SavedDeltaEventArgs : SaveDeltaBaseEventArgs
    {
        public SavedDeltaEventArgs(IDelta delta) : base(delta)
        {
           
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

    public class ProcessDeltaBaseEventArgs : SyncEventBase
    {
        public IDelta Delta { get; set; }

        public ProcessDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
}