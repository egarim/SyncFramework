using BIT.EfCore.Sync.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BIT.EfCore.Sync.Test.Contexts.SyncFramework
{
    public class SqlServerSyncDbContext : SyncFrameworkDbContext
    {
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="SqlServerSyncDbContext" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        protected SqlServerSyncDbContext()
        {

        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="SqlServerSyncDbContext" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public SqlServerSyncDbContext(DbContextOptions options, IServiceCollection ServiceCollection, string Identity) : base(options, ServiceCollection, Identity)
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //this.serviceProvider.WithSqliteDeltaGenerator().WithSqlServerDeltaGenerator();
            base.OnConfiguring(optionsBuilder);
        }
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="SqlServerSyncDbContext" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        //public SqlServerSyncDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        //{

        //}

        public DbSet<Blog> Blogs { get; set; }
    }
}
