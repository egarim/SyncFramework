
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Imp
{
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public class MemoryDeltaStore : DeltaStoreBase
    {
        readonly IList<IDelta> _Deltas;
        public IList<IDelta> Deltas => _Deltas;
        readonly IDictionary<string, SyncStatus> _syncStatus;
        Guid LastPushedDelta;
        Guid LastProcessedDelta;

        public MemoryDeltaStore(IEnumerable<IDelta> Deltas) : base()
        {
            this._Deltas = new List<IDelta>(Deltas);
            this._syncStatus = new Dictionary<string, SyncStatus>();

        }
        public MemoryDeltaStore() : this(new List<IDelta>())
        {

        }


        public async override Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)

        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (IDelta delta in deltas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Deltas.Add(new Delta(delta));
            }
        }

        public override Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(Guid startindex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
            return Task.FromResult(result.Cast<IDelta>());
        }
        public override Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(Guid startindex, string identity, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && d.Identity == identity) .ToList().Cast<IDelta>());
        }
        public override Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Deltas.Where(d => d.Index.CompareTo(startindex) > 0 ).ToList().Cast<IDelta>());
        }

        public override async Task<Guid> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            return _syncStatus[identity].LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _syncStatus.Add(identity, new SyncStatus() { LastProcessedDelta=Index,LastPushedDelta=Index });
            LastProcessedDelta = Index;


        }

        public async override Task<Guid> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken)
        {
            return _syncStatus[identity].LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_syncStatus.ContainsKey(identity))
                _syncStatus.Add(identity, new SyncStatus());
            _syncStatus[identity].LastPushedDelta = Index;


        }

        public async override Task<int> GetDeltaCountAsync(Guid startindex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Deltas.Count(d => d.Index.CompareTo(startindex) > 0 && d.Identity == identity);
        }

        public async override Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deltas = Deltas.Where(d => d.Identity == identity);
            foreach (var delta in deltas)
            {
                Deltas.Remove(delta);
            }
        }

        public override Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        {
           _syncStatus.Remove(identity);
            return Task.CompletedTask;
        }

        public override Task<bool> CanRestoreDatabaseAsync(string identity, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_syncStatus.ContainsKey(identity))
            {
                return Task.FromResult(false);                
            }
            var status = _syncStatus[identity];
            return Task.FromResult(status != null);
        }


#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
    class SyncStatus
    {
        public int LastTransactionLogProcessed { get; set; }
        public Guid LastProcessedDelta { get; set; }
        public Guid LastPushedDelta { get; set; }

    }
}
