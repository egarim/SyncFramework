using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ExtensionSyncFrameworkForSqlServer
    {
        public static IServiceCollection AddSyncFrameworkForSqlServer(this IServiceCollection serviceCollection,string sqlServerDeltaStoreConnectionString, HttpClient httpClient, string ServerNodeId, string Identity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            serviceCollection.AddEfSynchronization((options) => 
            { 
                options.UseSqlServer(sqlServerDeltaStoreConnectionString); },
                httpClient,
                ServerNodeId, 
                Identity,
                AdditionalDeltaGenerators);
            serviceCollection.AddEntityFrameworkSqlServer();
            return serviceCollection;

        }
    }
}
