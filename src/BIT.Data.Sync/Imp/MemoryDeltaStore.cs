
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
        Guid LastPushedDelta;
        Guid LastProcessedDelta;
      
        public MemoryDeltaStore(IEnumerable<IDelta> Deltas) : base()
        {
            this._Deltas = new List<IDelta>(Deltas);
            
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
        public override Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Deltas.Where(d => d.Index.CompareTo(startindex) > 0).ToList().Cast<IDelta>());
        }
      
        public override async Task<Guid> GetLastProcessedDeltaAsync(CancellationToken cancellationToken = default)
        {
            return LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(Guid Index, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastProcessedDelta = Index;


        }

        public async override Task<Guid> GetLastPushedDeltaAsync(CancellationToken cancellationToken)
        {
            return LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(Guid Index, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastPushedDelta = Index;


        }

        public async override Task<int> GetDeltaCountAsync(Guid startindex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Deltas.Count(d => d.Index.CompareTo(startindex) > 0);
        }

        public async override Task PurgeDeltasAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Deltas.Clear();
           

        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
