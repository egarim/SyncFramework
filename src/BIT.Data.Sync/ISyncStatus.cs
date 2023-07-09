using System;

namespace BIT.Data.Sync
{
    public interface ISyncStatus
    {
    
        string Identity { get; set; }
        string LastProcessedDelta { get; set; }
        string LastPushedDelta { get; set; }
   
    }
}