using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.EventArgs
{
    public abstract class BaseSyncEvent : System.EventArgs
    {

        protected BaseSyncEvent()
        {

            Data = new Dictionary<string, object>();
        }
        public Dictionary<string, object> Data { get; set; }
    }
}