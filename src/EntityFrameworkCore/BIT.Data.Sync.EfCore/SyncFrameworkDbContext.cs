using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace BIT.EfCore.Sync
{
    public abstract class SyncFrameworkDbContext : DbContext, ISyncClientNode
    {
        /// <summary>
        /// <para>
        /// Initializes a new instance of the <see cref="SyncFrameworkDbContext" /> class. The
        /// <see cref="M:Microsoft.EntityFrameworkCore.DbContext.OnConfiguring(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder)" />
        /// method will be called to configure the database (and other options) to be used for this context.
        /// </para>
        /// </summary>
        #region ctor
        protected SyncFrameworkDbContext()
        {

        }
        public SyncFrameworkDbContext(DbContextOptions options,
            IServiceProvider serviceProvider
            ) : base(options)
        {
            this.serviceProvider = serviceProvider;
            this.DeltaProcessor = new EFDeltaProcessor(this);
        }

        protected SyncFrameworkDbContext(DbContextOptions options) : base(options)
        {
        }
        #endregion
        public ISyncFrameworkClient SyncFrameworkClient { get; private set; }
        public IDeltaStore DeltaStore { get; private set; }
        public IDeltaProcessor DeltaProcessor { get; private set; }

        protected IServiceProvider serviceProvider;
        public string Identity { get; private set; }
  


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            this.Identity = serviceProvider.GetService<ISyncIdentityService>()?.Identity;
            this.DeltaStore = serviceProvider.GetService<IDeltaStore>();
            this.SyncFrameworkClient = serviceProvider.GetService<ISyncFrameworkClient>();
            var IEFSyncFrameworkService = serviceProvider.GetService<IModificationCommandToCommandDataService>();
            IEFSyncFrameworkService.RegisterDeltaGenerators(serviceProvider);
            optionsBuilder.UseInternalServiceProvider(serviceProvider);
        }

    }
}
