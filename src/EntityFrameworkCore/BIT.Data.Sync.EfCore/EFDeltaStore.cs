using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync
{

    public class EFDeltaStore : DeltaStoreBase
    {
        DeltaDbContext DeltaDbContext;

        public EFDeltaStore(DeltaDbContext DeltaDbContext)
        {
            this.DeltaDbContext = DeltaDbContext;
            this.DeltaDbContext.Database.EnsureCreated();
        }

        protected EFDeltaStore()
        {
            DbContextOptionsBuilder<DeltaDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<DeltaDbContext>();
            dbContextOptionsBuilder.UseInMemoryDatabase("MemoryDb");
            this.DeltaDbContext = new DeltaDbContext(dbContextOptionsBuilder.Options);
            this.DeltaDbContext.Database.EnsureCreated();
        }

        public async override Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (IDelta delta in deltas)
            {
                DeltaDbContext.Deltas.Add(new EFDelta(delta));
            }
            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        }

     
        public override async Task<Guid> GetLastProcessedDeltaAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return Guid.Empty;
            else
                return CurrentDeltaIndex.LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(Guid Index, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EFSyncStatus();
                DeltaDbContext.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastProcessedDelta = Index;

            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0).ToList().Cast<IDelta>());
        }

        public async override Task<Guid> GetLastPushedDeltaAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return Guid.Empty;
            else
                return CurrentDeltaIndex.LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(Guid Index, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EFSyncStatus();
                DeltaDbContext.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastPushedDelta = Index;

            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async override Task<int> GetDeltaCountAsync(Guid startindex, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await DeltaDbContext.Deltas.CountAsync(d => d.Index.CompareTo(startindex) > 0);
        }

        public async override Task PurgeDeltasAsync(CancellationToken cancellationToken=default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeltaDbContext.RemoveRange(DeltaDbContext.Deltas);
            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        }

        public override Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(Guid startindex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IQueryable<EFDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
            List<EFDelta> eFDeltas = result.ToList();
            return Task.FromResult(eFDeltas.Cast<IDelta>());
        }
    }


}