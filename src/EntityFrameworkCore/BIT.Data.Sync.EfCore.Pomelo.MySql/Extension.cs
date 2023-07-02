using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Pomelo.MySql
{
    public static class Extension
    {
        public static IServiceCollection AddSyncFrameworkForMysql(this IServiceCollection serviceCollection,string MySqlDeltaStoreConnectionString,ServerVersion serverVersion, HttpClient httpClient, string ServerNodeId, string Identity, params DeltaGeneratorBase[] AdditionalDeltaGenerators)
        {
            serviceCollection.AddEfSynchronization((options) => 
            { 
                options.UseMySql(MySqlDeltaStoreConnectionString, serverVersion); },
                httpClient,
                ServerNodeId, 
                Identity,
                AdditionalDeltaGenerators);
            serviceCollection.AddEntityFrameworkMySql();
            return serviceCollection;

        }
    }
}
