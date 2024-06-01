
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
        string LastPushedDelta;
        //string LastProcessedDelta;

        public MemoryDeltaStore(IEnumerable<IDelta> Deltas) : base(new MemorySequenceService(new YearSequencePrefixStrategy()))
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
                var SavingEventArgs = new SavingDeltaEventArgs(delta);

                // Raise the event
                OnSavingDelta(SavingEventArgs);

                // Check if the event handling should be canceled
                if (!SavingEventArgs.Handled)
                {
                    await SetDeltaIndex(delta);
                    cancellationToken.ThrowIfCancellationRequested();
                    Delta item = new Delta(delta);
                    item.Index = delta.Index;
                    Deltas.Add(item);
                    SavedDeltaEventArgs saveDeltaBaseEventArgs = new SavedDeltaEventArgs(delta);
                    OnSavedDelta(saveDeltaBaseEventArgs);
                }
            }
           
        }

        public override Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Deltas.Where(d => string.Compare(d.Index, startIndex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
            return Task.FromResult(result.Cast<IDelta>());
        }
        public override Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Deltas.Where(d => string.Compare(d.Index, startIndex) > 0 && d.Identity == identity) .ToList().Cast<IDelta>());
        }
        public override Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Deltas.Where(d => string.Compare(d.Index, startIndex) > 0).ToList().Cast<IDelta>());
        }

        public override async Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            return _syncStatus[identity].LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(_syncStatus.ContainsKey(identity))
            {
                _syncStatus[identity].LastProcessedDelta = Index;
            }
            else
            {
                _syncStatus.Add(identity, new SyncStatus() { LastProcessedDelta = Index, LastPushedDelta = Index });
            }
        
            


        }

        public async override Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken)
        {
            if (!_syncStatus.ContainsKey(identity))
                return string.Empty;

            return _syncStatus[identity].LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_syncStatus.ContainsKey(identity))
                _syncStatus.Add(identity, new SyncStatus());
            _syncStatus[identity].LastPushedDelta = Index;


        }

        public async override Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Deltas.Count(d => d.Index.CompareTo(startIndex) > 0 && d.Identity == identity);
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
        public string LastProcessedDelta { get; set; }
        public string LastPushedDelta { get; set; }

    }
}
