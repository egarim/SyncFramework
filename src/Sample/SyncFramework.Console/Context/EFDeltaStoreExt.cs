using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Context
{

    public class EFDeltaStoreExt : EFDeltaStore
    {
        EfDeltaDbContext DeltaDbContext;

        public EFDeltaStoreExt(EfDeltaDbContext DeltaDbContext) : base(DeltaDbContext)
        {
            this.DeltaDbContext = DeltaDbContext;
            this.DeltaDbContext.Database.EnsureCreated();
        }

        

        //public async override Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    foreach (IDelta delta in deltas)
        //    {
        //        DeltaDbContext.Deltas.Add(new EFDelta(delta));
        //    }
        //    await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        //}
        //public override Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(Guid startindex, string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    return Task.FromResult(DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && d.Identity == identity).ToList().Cast<IDelta>());
        //}
        //public override async Task<Guid> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        //    if (CurrentDeltaIndex == null)
        //        return Guid.Empty;
        //    else
        //        return CurrentDeltaIndex.LastProcessedDelta;
        //}

        //public override async Task SetLastProcessedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        //    if (CurrentDeltaIndex == null)
        //    {
        //        CurrentDeltaIndex = new EFSyncStatus()
        //        {
        //            Identity = identity
        //        };
        //        DeltaDbContext.Add(CurrentDeltaIndex);
        //    }

        //    CurrentDeltaIndex.LastProcessedDelta = Index;

        //    await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        //}

        //public override Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    return Task.FromResult(DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0).ToList().Cast<IDelta>());
        //}

        //public async override Task<Guid> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        //    if (CurrentDeltaIndex == null)
        //        return Guid.Empty;
        //    else
        //        return CurrentDeltaIndex.LastPushedDelta;
        //}

        //public async override Task SetLastPushedDeltaAsync(Guid Index, string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        //    if (CurrentDeltaIndex == null)
        //    {
        //        CurrentDeltaIndex = new EFSyncStatus()
        //        {
        //            Identity = identity
        //        };
        //        DeltaDbContext.Add(CurrentDeltaIndex);
        //    }

        //    CurrentDeltaIndex.LastPushedDelta = Index;

        //    await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        //}

        //public async override Task<int> GetDeltaCountAsync(Guid startindex, string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    return await DeltaDbContext.Deltas.CountAsync(d => d.Index.CompareTo(startindex) > 0 && d.Identity == identity);
        //}

        //public async override Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var deltas = DeltaDbContext.Deltas.Where(d => d.Identity == identity);
        //    DeltaDbContext.RemoveRange(deltas);
        //    await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        //}

        //public override Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(Guid startindex, string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    /* By default there is no Ef translation set for string.Compare in PostgreSql. luckily the PostgreSql is by default case sensitive */
        //    //IQueryable<EFDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
        //    IQueryable<EFDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startindex) > 0 && d.Identity != identity);
        //    List<EFDelta> eFDeltas = result.ToList();
        //    return Task.FromResult(eFDeltas.Cast<IDelta>());
        //}
        //public override async Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        //{
        //    cancellationToken.ThrowIfCancellationRequested();
        //    var status = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(d => d.Identity == identity, cancellationToken: cancellationToken).ConfigureAwait(false);
        //    if (status != null)
        //    {
        //        DeltaDbContext.RemoveRange(status);
        //        await DeltaDbContext.SaveChangesAsync(cancellationToken);
        //    }
        //}
    }


}