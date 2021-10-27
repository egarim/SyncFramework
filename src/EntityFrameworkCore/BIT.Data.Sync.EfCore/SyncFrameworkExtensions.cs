using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace BIT.EfCore.Sync
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
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions, HttpClient httpClient, string ServerNodeId, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(httpClient, ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaStoreDbContextOptions, syncFrameworkClient, AdditionalDeltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions, string ServerNodeId, string ServerUrl, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(ServerUrl,ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaStoreDbContextOptions, syncFrameworkClient, AdditionalDeltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaStoreDbContextOptions, ISyncFrameworkClient SyncFrameworkClient, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {

            serviceCollection.AddSingleton(typeof(ISyncFrameworkClient), SyncFrameworkClient);
            serviceCollection.AddScoped<IBatchExecutor, SyncFrameworkBatchExecutor>();
            serviceCollection.AddDbContext<DeltaDbContext>(DeltaStoreDbContextOptions);
            serviceCollection.AddSingleton<IDeltaStore, EFDeltaStore>();
            serviceCollection.AddSingleton<IModificationCommandToCommandData>(new ModificationCommandToCommandData(AdditionalDeltaGenerators));

            Dictionary<string, string> KnownUpdaters = new Dictionary<string, string>();
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.Sqlite.Update.Internal.SqliteUpdateSqlGenerator", "Sqlite");
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.SqlServer.Update.Internal.SqlServerUpdateSqlGenerator", "SqlServer");
            KnownUpdaters.Add("Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlUpdateSqlGenerator", "Postgres");
            
            serviceCollection.AddSingleton<IUpdaterAliasService>(new UpdaterAliasService(KnownUpdaters));
            return serviceCollection;

        }

    }
}
