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

        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaContextOption, HttpClient httpClient, string ServerNodeId, params DeltaGeneratorBase[] deltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(httpClient, ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaContextOption, syncFrameworkClient, deltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaContextOption, string ServerNodeId, string ServerUrl, params DeltaGeneratorBase[] deltaGenerators)
        {
            SyncFrameworkHttpClient syncFrameworkClient = new SyncFrameworkHttpClient(ServerUrl,ServerNodeId);
            return serviceCollection.AddEfSynchronization(DeltaContextOption, syncFrameworkClient, deltaGenerators);
        }
        public static IServiceCollection AddEfSynchronization(this IServiceCollection serviceCollection, Action<DbContextOptionsBuilder> DeltaContextOption, ISyncFrameworkClient SyncFrameworkClient, params DeltaGeneratorBase[] deltaGenerators)
        {

            serviceCollection.AddSingleton(typeof(ISyncFrameworkClient), SyncFrameworkClient);
            serviceCollection.AddScoped<IBatchExecutor, SyncFrameworkBatchExecutor>();
            serviceCollection.AddDbContext<DeltaDbContext>(DeltaContextOption);
            serviceCollection.AddSingleton<IDeltaStore, EFDeltaStore>();
            serviceCollection.AddSingleton<IEfCommandDataGeneratorService>(new EfCommandDataGeneratorService(deltaGenerators));

            Dictionary<string, string> KnownUpdaters = new Dictionary<string, string>();
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.Sqlite.Update.Internal.SqliteUpdateSqlGenerator", "Sqlite");
            KnownUpdaters.Add("Microsoft.EntityFrameworkCore.SqlServer.Update.Internal.SqlServerUpdateSqlGenerator", "SqlServer");
            KnownUpdaters.Add("Npgsql.EntityFrameworkCore.PostgreSQL.Update.Internal.NpgsqlUpdateSqlGenerator", "Postgres");
            serviceCollection.AddSingleton<IUpdaterAliasService>(new UpdaterAliasService(KnownUpdaters));
            return serviceCollection;

        }

    }
}
