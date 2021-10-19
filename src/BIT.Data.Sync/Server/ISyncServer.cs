
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServer
    {
        IEnumerable<ISyncServerNode> Nodes { get; }
        Task<IEnumerable<IDelta>> GetDeltasAsync(string name, Guid startindex, string identity, CancellationToken cancellationToken);
        Task ProcessDeltasAsync(string Name, IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
        Task SaveDeltasAsync(string name, IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    }
}
