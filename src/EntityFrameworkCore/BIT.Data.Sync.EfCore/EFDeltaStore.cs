using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using BIT.Data.Sync.EventArgs;
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
    /// <summary>
    /// A delta store backed by a <see cref="BIT.EfCore.Sync.DeltaDbContext"/>.
    /// Every operation creates and disposes its own context, so one instance (for example a DI singleton) can serve concurrent calls.
    /// </summary>
    public class EfDeltaStore : DeltaStoreBase
    {


        public static bool EnsureDeleted { get; set; }
        /// <summary>
        /// The context passed to the constructor, kept for existing callers. The store never uses it for queries or saves;
        /// it only reads its type and options to create a new context per operation. Null when the store was built from a factory.
        /// </summary>
        public DeltaDbContext DeltaDbContext { get; protected set; }

        private readonly Func<DeltaDbContext> contextFactory;

        [Obsolete("use the constructor with DbContextOptionsBuilder or DeltaDbContext directly instead. This constructor is for testing purposes only and may be removed in future versions.",true)]
        public EfDeltaStore(DeltaDbContext DeltaDbContext):base(DeltaDbContext.GetService<ISequenceService>())
        {
            this.DeltaDbContext = DeltaDbContext;
            this.contextFactory = DeltaDbContextFactory.FromTemplate(DeltaDbContext);
            EnsureDatabase();
        }
        /// <summary>
        /// Creates a store that uses the type and options of <paramref name="DeltaDbContext"/> to create a new context for each operation.
        /// </summary>
        public EfDeltaStore(DeltaDbContext DeltaDbContext, ISequenceService sequenceService) : base(sequenceService)
        {
            this.DeltaDbContext = DeltaDbContext;
            this.contextFactory = DeltaDbContextFactory.FromTemplate(DeltaDbContext);
            EnsureDatabase();
        }
        /// <summary>
        /// Creates a store that calls <paramref name="contextFactory"/> for a new context in each operation and disposes it afterwards.
        /// </summary>
        public EfDeltaStore(Func<DeltaDbContext> contextFactory, ISequenceService sequenceService) : base(sequenceService)
        {
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            EnsureDatabase();
        }

        private void EnsureDatabase()
        {
            using var context = CreateContext();
            //HACK TEST set EnsureDeleted to ensure a clean delta database for testing
            if (EnsureDeleted)
            {
                context.Database.EnsureDeleted();
            }

            context.Database.EnsureCreated();
        }

        /// <summary>
        /// A new context for one operation; the caller disposes it.
        /// </summary>
        protected virtual DeltaDbContext CreateContext() => contextFactory();

        public async override Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        {
            await using var context = CreateContext();
            foreach (IDelta delta in deltas)
            {

                // Create an instance of the custom EventArgs
                var SavingEventArgs = new SavingDeltaEventArgs(delta);

                // Raise the event
                OnSavingDelta(SavingEventArgs);

                // Check if the event handling should be canceled
                if (!SavingEventArgs.CustomHandled)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await SetDeltaIndex(delta).ConfigureAwait(false);
                    EfDelta entity = new EfDelta(delta);

                    context.Deltas.Add(entity);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                   
                }
                SavedDeltaEventArgs args = new SavedDeltaEventArgs(delta, SavingEventArgs.CustomHandled);
                OnSavedDelta(args);
            }
            

        }
        public override async Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
            EfSyncStatus CurrentDeltaIndex;
            await using (var context = CreateContext())
            {
                CurrentDeltaIndex = await context.EFSyncStatus.AsNoTracking().FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            }
            if (CurrentDeltaIndex == null)
                return await this.SequenceService.GetFirstIndexValue().ConfigureAwait(false);
            if (CurrentDeltaIndex.LastProcessedDelta==default)
                return await this.SequenceService.GetFirstIndexValue().ConfigureAwait(false);
            else
                return CurrentDeltaIndex.LastProcessedDelta;
        }

        public override async Task SetLastProcessedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            var CurrentDeltaIndex = await context.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EfSyncStatus()
                {
                    Identity = identity
                };
                context.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastProcessedDelta = Index;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default)
        {
            startIndex = GuardStartIndex(startIndex);
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            var deltas = await context.Deltas.AsNoTracking().Where(d => d.Index.CompareTo(startIndex) > 0).ToListAsync(cancellationToken).ConfigureAwait(false);
            return deltas.Cast<IDelta>();
        }
  
        public override async Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
          
            cancellationToken.ThrowIfCancellationRequested();
            startIndex=GuardStartIndex(startIndex);
            await using var context = CreateContext();
            var deltas = await context.Deltas.AsNoTracking().Where(d => string.Compare(d.Index, startIndex) > 0 && d.Identity == identity).ToListAsync(cancellationToken).ConfigureAwait(false);
            return deltas.Cast<IDelta>();
        }

        public async override Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EfSyncStatus CurrentDeltaIndex;
            await using (var context = CreateContext())
            {
                CurrentDeltaIndex = await context.EFSyncStatus.AsNoTracking().FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            }
            if (CurrentDeltaIndex == null)
                return await this.SequenceService.GetFirstIndexValue().ConfigureAwait(false);
            else
                return CurrentDeltaIndex.LastPushedDelta;
        }

        public async override Task SetLastPushedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            var CurrentDeltaIndex = await context.EFSyncStatus.FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new EfSyncStatus()
                {
                    Identity = identity
                };
                context.Add(CurrentDeltaIndex);
            }

            CurrentDeltaIndex.LastPushedDelta = Index;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async override Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            return await context.Deltas.CountAsync(d => d.Index.CompareTo(startIndex) > 0 && d.Identity == identity, cancellationToken).ConfigureAwait(false);
        }

        public async override Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            var deltas = context.Deltas.Where(d => d.Identity == identity);
            context.RemoveRange(deltas);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        }

        public override async Task<IEnumerable<IDelta>> GetDeltasFromOtherClients(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            /* By default there is no Ef translation set for string.Compare in PostgreSql. luckily the PostgreSql is by default case sensitive */
            //IQueryable<EFDelta> result = DeltaDbContext.Deltas.Where(d => d.Index.CompareTo(startIndex) > 0 && string.Compare(d.Identity, identity, StringComparison.Ordinal) != 0);
            IQueryable<EfDelta> result = context.Deltas.AsNoTracking().Where(d => d.Index.CompareTo(startIndex) > 0 && d.Identity != identity);
            List<EfDelta> eFDeltas = await result.ToListAsync(cancellationToken).ConfigureAwait(false);
            return eFDeltas.Cast<IDelta>();
        }

        public override async Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var context = CreateContext();
            var status = await context.EFSyncStatus.FirstOrDefaultAsync(d => d.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (status != null)
            {
                context.RemoveRange(status);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken)
        {
            await using var context = CreateContext();
            return await context.Deltas.AsNoTracking().FirstOrDefaultAsync(d => d.Index == deltaId, cancellationToken).ConfigureAwait(false);
        }

        public async override Task PurgeDeltaStoreAsync(CancellationToken cancellationToken)
        {
            await using var context = CreateContext();
            await context.Deltas.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.EFSyncStatus.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.EfSequence.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return; 
        }
    }


}
