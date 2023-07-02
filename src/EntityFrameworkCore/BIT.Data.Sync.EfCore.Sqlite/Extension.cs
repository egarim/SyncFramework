using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.SQLite
{
    public static class Extension
    {
        public static IServiceCollection AddSyncFrameworkForSQLite(this IServiceCollection serviceCollection,string SQliteDeltaStoreConnectionString, HttpClient httpClient, string ServerNodeId, string Identity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            serviceCollection.AddEfSynchronization((options) => 
            { 
                options.UseSqlite(SQliteDeltaStoreConnectionString); },
                httpClient,
                ServerNodeId, 
                Identity,
                AdditionalDeltaGenerators);
            serviceCollection.AddEntityFrameworkSqlite();
            return serviceCollection;

        }
    }
}
