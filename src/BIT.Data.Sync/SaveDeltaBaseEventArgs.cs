
using BIT.Data.Sync.Server;
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
    public class SyncServerSavedDeltaEventArgs: SavedDeltaEventArgs
    {

        public SyncServerSavedDeltaEventArgs(IDelta delta) : base(delta)
        {
        }
        public SyncServerSavedDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
    }
    public class SyncServerSavingDeltaEventArgs : SavingDeltaEventArgs
    {
        public SyncServerSavingDeltaEventArgs(IDelta delta) : base(delta)
        {

        }
        public SyncServerSavingDeltaEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
        public ISyncServerNode Node { get; set; }
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
    public class SyncServerProcessingDeltaEventArgs : ProcessingDeltaEventArgs
    {
        public SyncServerProcessingDeltaEventArgs(IDelta delta) : base(delta)
        {
            this.Handled = false;
        }
        public SyncServerProcessingDeltaEventArgs(IDelta delta, ISyncServerNode node) : this(delta)
        {
            Node = node;
          
        }
        public ISyncServerNode Node { get; set; }

   
    }
    public class ProcessDeltaBaseEventArgs : SyncEventBase
    {
        public IDelta Delta { get; set; }

        public ProcessDeltaBaseEventArgs(IDelta delta)
        {
            Delta = delta;
        }
    }
    public class SyncServerProcessDeltaBaseEventArgs : ProcessDeltaBaseEventArgs
    {
        public SyncServerProcessDeltaBaseEventArgs(IDelta delta) : base(delta)
        {

        }
        public ISyncServerNode Node { get; set; }
        public SyncServerProcessDeltaBaseEventArgs(IDelta delta, ISyncServerNode node) : base(delta)
        {
            Node = node;
        }
    }
}