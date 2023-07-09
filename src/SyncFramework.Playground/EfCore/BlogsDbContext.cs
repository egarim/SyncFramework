using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BIT.EfCore.Sync;
using System;

namespace SyncFramework.Playground.EfCore
{
    public class BlogsDbContext : SyncFrameworkDbContext
    {
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="BlogsDbContext" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        protected BlogsDbContext()
        {

        }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="BlogsDbContext" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public BlogsDbContext(DbContextOptions options, IServiceProvider SyncFrameworkServiceCollection) : base(options, SyncFrameworkServiceCollection)
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
