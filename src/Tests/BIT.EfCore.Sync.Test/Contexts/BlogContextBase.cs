using BIT.EfCore.Sync.Test.Model;
using Microsoft.EntityFrameworkCore;
using System;
public class BlogContextBase : DbContext
{
    /// <summary>
    /// <para>
    /// Initializes a new instance of the <see cref="BlogContextBase" /> class. The
    /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
    /// method will be called to configure the database (and other options) to be used for this context.
    /// </para>
    /// </summary>
    protected BlogContextBase()
    {

    }

    /// <summary>
    /// <para>
    /// Initializes a new instance of the <see cref="BlogContextBase" /> class using the specified options.
    /// The <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" /> method will still be called to allow further
    /// configuration of the options.
    /// </para>
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public BlogContextBase(DbContextOptions options) : base(options)
    {

    }
    protected IServiceProvider serviceProvider;
    public BlogContextBase(IServiceProvider serviceProvider)

    {
        this.serviceProvider = serviceProvider;
    }
    public DbSet<Blog> Blogs { get; set; }


}
