using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{

    public static class AspNetCoreServiceExtensions
    {
        public static IServiceCollection AddSyncServerWithMemoryNode(this IServiceCollection serviceCollection, string NodeId)
        {
            SyncServerNode syncServerNode = new SyncServerNode(new MemoryDeltaStore(), null, NodeId);
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(syncServerNode));
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithNodes(this IServiceCollection serviceCollection, params ISyncServerNode[] Nodes)
        {
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(Nodes));
            return serviceCollection;
        }
    }
}
