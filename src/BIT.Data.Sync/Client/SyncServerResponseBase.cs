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

        public string ServerNodeId { get; set; }
        public string ClientNodeId { get; set; }

    }
    public class SyncServerRequestBase
    {
        public string Message { get; set; }
        public bool Success { get; set; }



    }
    public class PushOperationResponse: SyncServerResponseBase
    {
     


        public List<string> ProcessedDeltasIds { get; set; } = new List<string>();
    
    }
    public class HandShakeOperationResponse : SyncServerResponseBase
    {

        public HandShakeOperationResponse()
        {
        
        }
    
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
