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
    public static class ExtensionSyncFrameworkForNpgsql
    {
        public static IServiceCollection AddSyncFrameworkForNpgsql(this IServiceCollection serviceCollection,string NpgsqlDeltaStoreConnectionString, HttpClient httpClient, string ServerNodeId, string Identity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            serviceCollection.AddEfSynchronization((options) => 
            { 
                options.UseNpgsql(NpgsqlDeltaStoreConnectionString); },
                httpClient,
                ServerNodeId, 
                Identity,
                AdditionalDeltaGenerators);
            serviceCollection.AddEntityFrameworkNpgsql();
            return serviceCollection;

        }
    }
}
