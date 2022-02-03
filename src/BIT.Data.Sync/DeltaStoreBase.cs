
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public abstract class DeltaStoreBase : IDeltaStore
    {

        protected DeltaStoreBase()
        {

        }
   

        public abstract Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default);

        public abstract Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(Guid startindex, string identity, CancellationToken cancellationToken = default);
        public abstract Task<Guid> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default);
        public abstract Task SetLastProcessedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default);

        public abstract Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, CancellationToken cancellationToken = default);
        public abstract Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(Guid startindex, string identity, CancellationToken cancellationToken = default);

        public abstract Task<Guid> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default);
        public abstract Task SetLastPushedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default);

        public abstract Task<int> GetDeltaCountAsync(Guid startindex, string identity, CancellationToken cancellationToken=default);
        public abstract Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken=default);

        public abstract Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken=default);

        public abstract Task<bool> CanRestoreDatabaseAsync(string identity, CancellationToken cancellationToken);
        
    }
}