using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync
{

    /// <summary>
    /// Hands out delta indexes stored in the EfSequence table of a <see cref="DeltaDbContext"/>.
    /// Every call uses its own context, and calls for the same database and prefix are serialized process-wide,
    /// so concurrent callers (even through different instances) never get the same value and no value is skipped.
    /// </summary>
    public class EfSequenceService : SequenceServiceBase, ISequenceService
    {
        // One lock per database and prefix, shared by every instance in the process.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private readonly Func<DeltaDbContext> contextFactory;

        /// <summary>
        /// Creates a service that uses the type and options of <paramref name="deltaDbContext"/> to create a new context for each call.
        /// The context itself is not used for queries or saves.
        /// </summary>
        public EfSequenceService(ISequencePrefixStrategy sequencePrefixStrategy, DeltaDbContext deltaDbContext) : base(sequencePrefixStrategy)
        {
            contextFactory = DeltaDbContextFactory.FromTemplate(deltaDbContext);
        }

        /// <summary>
        /// Creates a service that calls <paramref name="contextFactory"/> for a new context in each call and disposes it afterwards.
        /// </summary>
        public EfSequenceService(ISequencePrefixStrategy sequencePrefixStrategy, Func<DeltaDbContext> contextFactory) : base(sequencePrefixStrategy)
        {
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public override async Task<string> GenerateNextSequenceAsync(string prefix)
        {
            await using var context = contextFactory();
            bool relational = context.Database.IsRelational();
            // In-memory stores have no connection string; they live in this process, so one lock for all of them is enough.
            string database = relational ? context.Database.GetConnectionString() : "<non-relational>";
            var gate = locks.GetOrAdd($"{context.GetType().FullName}|{database}|{prefix}", _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync().ConfigureAwait(false);
            try
            {
                int number = relational
                    ? await IncrementInTransactionAsync(context, prefix).ConfigureAwait(false)
                    : await IncrementTrackedAsync(context, prefix).ConfigureAwait(false);
                return $"{prefix}{number:D10}";
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Increments with one UPDATE and reads the result in the same transaction; the row stays locked until commit,
        /// so other processes using the same database wait instead of reading the same value.
        /// </summary>
        private static async Task<int> IncrementInTransactionAsync(DeltaDbContext context, string prefix)
        {
            await using var transaction = await context.Database.BeginTransactionAsync().ConfigureAwait(false);
            int updated = await context.EfSequence.Where(s => s.Prefix == prefix)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.LastNumber, x => x.LastNumber + 1)).ConfigureAwait(false);
            if (updated == 0)
            {
                context.EfSequence.Add(new EfSequence { Prefix = prefix, LastNumber = 1 });
                await context.SaveChangesAsync().ConfigureAwait(false);
            }
            // Max: if an older version left more than one row for the prefix, all were incremented and the max is still unused.
            int number = await context.EfSequence.Where(s => s.Prefix == prefix).MaxAsync(s => s.LastNumber).ConfigureAwait(false);
            await transaction.CommitAsync().ConfigureAwait(false);
            return number;
        }

        private static async Task<int> IncrementTrackedAsync(DeltaDbContext context, string prefix)
        {
            var sequence = await context.EfSequence.FirstOrDefaultAsync(s => s.Prefix == prefix).ConfigureAwait(false);
            if (sequence == null)
            {
                sequence = new EfSequence { Prefix = prefix, LastNumber = 0 };
                context.EfSequence.Add(sequence);
            }

            sequence.LastNumber++;

            await context.SaveChangesAsync().ConfigureAwait(false);
            return sequence.LastNumber;
        }


    }

}
