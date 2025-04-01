using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Client
{
    public class SyncServerResponseBase
    {
        public string Message { get; set; }
        public bool Success { get; set; }



    }
    public class SyncServerRequestBase
    {
        public string Message { get; set; }
        public bool Success { get; set; }



    }
    public class PushOperationResponse: SyncServerResponseBase
    {
     


    }
    public class FetchOperationResponse : SyncServerResponseBase
    {
        public FetchOperationResponse()
        {
            Deltas = new List<Delta>(); 
        }
        public List<Delta> Deltas { get; set; }
    }
}
