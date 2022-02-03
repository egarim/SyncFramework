using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using SyncFramework.ConsoleApp.Context;
using SyncFramework.ConsoleApp.Helper;
using SyncFramework.ConsoleApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Extensions
{
    public static class SyncFrameworkExtensions
    {

        /// <summary>
        /// Add the necessary services to save, process and transport delta information in a synchronization network
        /// </summary>
        /// <param name="serviceCollection">The current service collection</param>
        /// <param name="DeltaStoreDbContextOptions">The options for the delta store DbContext</param>
        /// <param name="httpClient">The HTTP client for network communication</param>
        /// <param name="ServerNodeId">The id of the node wh</param>
        /// <param name="AdditionalDeltaGenerators"></param>
        /// <returns></returns>
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions, HttpClient httpClient, string ServerNodeId, string Idenity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(httpClient, ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaStoreDbContextOptions, syncFrameworkClient, Idenity, AdditionalDeltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions, string ServerNodeId, string ServerUrl, string Idenity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(ServerUrl, ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaStoreDbContextOptions, syncFrameworkClient, Idenity, AdditionalDeltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection,
            Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions,
            ISyncFrameworkClient SyncFrameworkClient, string Identity,
            params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {

            serviceCollection.AddSingleton(typeof(ISyncFrameworkClient), SyncFrameworkClient);
            serviceCollection.AddScoped<IBatchExecutor, SyncFrameworkBatchExecutor>();
            serviceCollection.AddDbContext<EfDeltaDbContext>(DeltaStoreDbContextOptions);
            serviceCollection.AddSingleton<IDeltaStore, EFDeltaStoreExt>();
            serviceCollection.AddSingleton<IModificationCommandToCommandDataService>(new ModificationCommandToCommandDataService(AdditionalDeltaGenerators));
            serviceCollection.AddSingleton<ISyncIdentityService>(new SyncIdentityService(Identity));

            Dictionary<string, string> KnownUpdaters = new Dictionary<string, string>();
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.Sqlite.Update.Internal.SqliteUpdateSqlGenerator", "Sqlite");
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.SqlServer.Update.Internal.SqlServerUpdateSqlGenerator", "SqlServer");
            KnownUpdaters.Add("Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlUpdateSqlGenerator", "Postgres");
            KnownUpdaters.Add("Pomelo.EntityFrameworkCore.MySql.Update.Internal.MySqlUpdateSqlGenerator", "MySqlPomelo");
            serviceCollection.AddSingleton<IUpdaterAliasService>(new UpdaterAliasService(KnownUpdaters));
            /* Addition by Me (Qamar) added EFDeltaProcessorHelper to remover Dbcontext dependency*/
            //serviceCollection.AddSingleton<EFDeltaProcessorHelper>();
            //serviceCollection.AddSingleton<IClientSyncProvider, ClientSyncProvider>();

            return serviceCollection;

        }
    }
}
