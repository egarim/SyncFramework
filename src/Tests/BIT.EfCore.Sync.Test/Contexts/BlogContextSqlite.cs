using Microsoft.EntityFrameworkCore;
using System;

namespace BIT.EfCore.Sync.Test.Contexts
{
    public class BlogContextSqlite : BlogContextBase
    {

        public BlogContextSqlite()
        {

        }

        public BlogContextSqlite(DbContextOptions<BlogContextSqlite> options)
       : base(options)
        { }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="BlogContextSqlite" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public BlogContextSqlite(DbContextOptions options) : base(options)
        {

        }

        public BlogContextSqlite(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Data Source=Test.db;").UseInternalServiceProvider(serviceProvider);
        }
    }
}