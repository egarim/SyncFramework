using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;
namespace BIT.EfCore.Sync
{
    public class DeltaDbContext : DbContext
    {
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="DeltaDbContext" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        protected DeltaDbContext()
        {

        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="DeltaDbContext" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public DeltaDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<EfDelta> Deltas { get; set; }
        public DbSet<EfSyncStatus> EFSyncStatus { get; set; }
        public DbSet<EfSequence> EfSequence { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
      
        }
    }
}
