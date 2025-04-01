using BIT.Data.Sync;
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
        public static IServiceCollection AddSyncServerWithMemoryNode(this IServiceCollection serviceCollection, string NodeId, Func<RegisterNodeRequest, ISyncServerNode> RegisterNodeFunction)
        {
            SyncServerNode syncServerNode = new SyncServerNode(new MemoryDeltaStore(), null, NodeId);
            SyncServer implementationInstance = new SyncServer(syncServerNode);
            implementationInstance.RegisterNodeFunction = RegisterNodeFunction;
            serviceCollection.AddSingleton((ISyncServer)implementationInstance);
          
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithMemoryNode(this IServiceCollection serviceCollection, string NodeId)
        {
            SyncServerNode syncServerNode = new SyncServerNode(new MemoryDeltaStore(), null, NodeId);
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(syncServerNode));
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithDeltaStoreNode(this IServiceCollection serviceCollection,IDeltaStore  deltaStore ,string NodeId)
        {
            SyncServerNode syncServerNode = new SyncServerNode(deltaStore, null, NodeId);
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(syncServerNode));
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithDeltaStoreNode(this IServiceCollection serviceCollection, IDeltaStore deltaStore, string NodeId, Func<RegisterNodeRequest, ISyncServerNode> RegisterNodeFunction)
        {
            SyncServerNode syncServerNode = new SyncServerNode(deltaStore, null, NodeId);
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(syncServerNode) { RegisterNodeFunction= RegisterNodeFunction });
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithNodes(this IServiceCollection serviceCollection, params ISyncServerNode[] Nodes)
        {
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(Nodes));
            return serviceCollection;
        }
        public static IServiceCollection AddSyncServerWithNodes(this IServiceCollection serviceCollection, Func<RegisterNodeRequest, ISyncServerNode> RegisterNodeFunction, params ISyncServerNode[] Nodes)
        {
            serviceCollection.AddSingleton<ISyncServer>(new SyncServer(Nodes) { RegisterNodeFunction = RegisterNodeFunction });
            return serviceCollection;
        }
    }
}
