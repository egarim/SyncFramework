
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerNode
    {
        string NodeId { get; set; }
        Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
        Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, string identity, CancellationToken cancellationToken);
        Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    }

    
}
