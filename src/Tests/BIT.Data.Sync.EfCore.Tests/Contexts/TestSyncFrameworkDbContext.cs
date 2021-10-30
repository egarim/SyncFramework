using BIT.Data.Sync.EfCore.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BIT.EfCore.Sync;

namespace BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework
{
    public class TestSyncFrameworkDbContext : SyncFrameworkDbContext
    {
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="TestSyncFrameworkDbContext" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        protected TestSyncFrameworkDbContext()
        {

        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="TestSyncFrameworkDbContext" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public TestSyncFrameworkDbContext(DbContextOptions options, IServiceCollection SyncFrameworkServiceCollection) : base(options, SyncFrameworkServiceCollection)
        {
            //ServiceCollection.AddSingleton<ISyncIdentityService>(new SyncIdentityService(Identity));
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
            base.OnConfiguring(optionsBuilder);
        }
      
        public DbSet<Blog> Blogs { get; set; }
    }
}
