using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync
{
    public class EfDeltaStore : DeltaStoreBase
    {
        DeltaDbContext DeltaDbContext;
        public EfDeltaStore(DeltaDbContext DeltaDbContext):base(DeltaDbContext.GetService<ISequenceService>())
        {
            this.DeltaDbContext = DeltaDbContext;
            //HACK TEST remove the comment below to ensure a clean delta database for testing
            //this.DeltaDbContext.Database.EnsureDeleted();
            this.DeltaDbContext.Database.EnsureCreated();
          
        }
        public EfDeltaStore(DbContextOptionsBuilder<DeltaDbContext> dbContextOptionsBuilder):this(new DeltaDbContext(dbContextOptionsBuilder.Options))
        {
           
            
        }


        //TODO clean up later, constructor not needed

        //protected EfDeltaStore()
        //{
        //    DbContextOptionsBuilder<DeltaDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<DeltaDbContext>();
        //    dbContextOptionsBuilder.UseInMemoryDatabase("MemoryDb");
        //    this.DeltaDbContext = new DeltaDbContext(dbContextOptionsBuilder.Options);
        //    this.DeltaDbContext.Database.EnsureCreated();
        //}
     
        public async override Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        {
       
            foreach (IDelta delta in deltas)
            {

                // Create an instance of the custom EventArgs
                var SavingEventArgs = new SavingDeltaEventArgs(delta);

                // Raise the event
                OnDeltaSavingDelta(SavingEventArgs);

                // Check if the event handling should be canceled
                if (!SavingEventArgs.Handled)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await SetDeltaIndex(delta);
                    EfDelta entity = new EfDelta(delta);

                    DeltaDbContext.Deltas.Add(entity);
                    await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    SaveDeltaBaseEventArgs saveDeltaBaseEventArgs = new SaveDeltaBaseEventArgs(delta);
                    OnDeltaSavedDelta(saveDeltaBaseEventArgs);  
                }
             
            }
            

        }
        public override async Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return await this.SequenceService.GetFirstIndexValue();
            if (CurrentDeltaIndex.LastProcessedDelta==default)
                return await this.SequenceService.GetFirstIndexValue();
            else
                return CurrentDeltaIndex.LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EfSyncStatus()
                {
                    Identity = identity
                };
                DeltaDbContext.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastProcessedDelta = Index;

            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default)
        {
            startIndex = GuardStartIndex(startIndex);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startIndex) > 0).ToList().Cast<IDelta>());
        }
  
        public override Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
          
            cancellationToken.ThrowIfCancellationRequested();
            startIndex=GuardStartIndex(startIndex);
            IEnumerable<IDelta> result = DeltaDbContext.Deltas.Where(d => string.Compare(d.Index, startIndex) > 0 && d.Identity == identity).ToList().Cast<IDelta>();
             return Task.FromResult(result);
        }

        public async override Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return await this.SequenceService.GetFirstIndexValue();
            else
                return CurrentDeltaIndex.LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EfSyncStatus()
                {
                    Identity = identity
                };
                DeltaDbContext.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastPushedDelta = Index;

            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async override Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await DeltaDbContext.Deltas.CountAsync(d => d.Index.CompareTo(startIndex) > 0 && d.Identity == identity);
        }

        public async override Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deltas = DeltaDbContext.Deltas.Where(d => d.Identity == identity);
            DeltaDbContext.RemoveRange(deltas);
            await DeltaDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        }

        public override async Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            /* By default there is no Ef translation set for string.Compare in PostgreSql. luckily the PostgreSql is by default case sensitive */
            //IQueryable<EFDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startIndex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
            IQueryable<EfDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startIndex) > 0 && d.Identity != identity);
            List<EfDelta> eFDeltas = result.ToList();
            return await Task.FromResult(eFDeltas.Cast<IDelta>()).ConfigureAwait(false);
        }

        public override async Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await DeltaDbContext.EFSyncStatus.FirstOrDefaultAsync(d => d.Identity == identity).ConfigureAwait(false);
            if (status != null)
            {
                DeltaDbContext.RemoveRange(status);
                await DeltaDbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public override async Task<bool> CanRestoreDatabaseAsync(string identity, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await DeltaDbContext.EFSyncStatus.AnyAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        }
    }


}