using Microsoft.EntityFrameworkCore;
using System;

namespace BIT.EfCore.Sync.Test.Contexts
{
    public class BlogContextSqlServer : BlogContextBase
    {
        private const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=BlogsTest;Trusted_Connection=True";

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="BlogContextSqlServer" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        public BlogContextSqlServer()
        {

        }
        public BlogContextSqlServer(DbContextOptions<BlogContextSqlServer> options)
           : base(options)
        { }

        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="BlogContextSqlServer" /> class using the specified options.
        /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
        /// configuration of the options.
        /// </para>
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public BlogContextSqlServer(DbContextOptions options) : base(options)
        {

        }

        public BlogContextSqlServer(IServiceProvider serviceProvider) : base(serviceProvider)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlServer(ConnectionString).UseInternalServiceProvider(serviceProvider);
        }





    }
}