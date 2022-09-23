using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore;
using BIT.Data.Sync.EfCore.Data;
using BIT.EfCore.Sync;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using SyncFramework.ConsoleApp.Helper;
using SyncFramework.ConsoleApp.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Context
{

    public abstract class SyncDbContext : DbContext, ISyncClientNode
    {
        public ISyncFrameworkClient SyncFrameworkClient { get; private set; }
        public IDeltaStore DeltaStore { get; private set; }
        public IDeltaProcessor DeltaProcessor { get; private set; }

        protected IServiceProvider serviceProvider;
        public string Identity { get; private set; }
        public IModificationCommandToCommandDataService IEFSyncFrameworkService { get; private set; }

        #region Helper Methods
        protected static DbContextOptions<T> ChangeOptionsType<T>(DbContextOptions options) where T : DbContext
        {
            var sqlExt = options.Extensions.FirstOrDefault(e => e is SqlServerOptionsExtension);

            if (sqlExt == null)
                throw (new Exception("Failed to retrieve SQL connection string for base Context"));
            return new DbContextOptionsBuilder<T>()
                        .UseSqlServer(((SqlServerOptionsExtension)sqlExt).ConnectionString)
                        .Options;
        }
        private SqlServerOptionsExtension GetSqlServerExtension(DbContextOptions options)
        {
            return (SqlServerOptionsExtension)options.Extensions.FirstOrDefault(e => e is SqlServerOptionsExtension);
        } 
        #endregion

        public SyncDbContext(DbContextOptions options,
            IServiceProvider serviceProvider
            ) : base(options)
        {
            this.serviceProvider = serviceProvider;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {            
            this.Identity = serviceProvider.GetService<ISyncIdentityService>()?.Identity;
            this.DeltaStore = serviceProvider.GetService<IDeltaStore>();
            this.DeltaProcessor = new EFDeltaProcessor(this);
            this.SyncFrameworkClient = serviceProvider.GetService<ISyncFrameworkClient>();
            IEFSyncFrameworkService = serviceProvider.GetService<IModificationCommandToCommandDataService>();
            IEFSyncFrameworkService.RegisterDeltaGenerators(serviceProvider);
            optionsBuilder.UseInternalServiceProvider(serviceProvider);

        }
    }
}
