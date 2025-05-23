
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{
    public class SyncServerNodeList:List<IServerSyncEndpoint>
    {
        
        public SyncServerNodeList()
        {
            
        }
    }
    public interface IInfoServer
    {
        
    }
    public class SyncServer : ISyncServer, ISyncServerWithEvents
    {
        public SyncServer(IEnumerable<IServerSyncEndpoint> nodes, Func<RegisterNodeRequest, IServerSyncEndpoint> registerNodeRequest,IInfoServer infoServer)
        {
            Nodes.AddRange(nodes);
            foreach (IServerSyncEndpoint syncServerNode in Nodes)
            {
                RegisterEvents(syncServerNode as ISyncServerNodeWithEvents, syncServerNode);
               
            }
            RegisterNodeFunction = registerNodeRequest;
        }
        public SyncServer(params IServerSyncEndpoint[] Nodes) : this(Nodes, null, null)
        {

        }
        public SyncServer(SyncServerNodeList Nodes) : this(Nodes.ToArray(), null, null)
        {

        }
        protected SyncServer()
        {
         
        }
   
        public List<IServerSyncEndpoint> Nodes { get; } = new List<IServerSyncEndpoint>();
        public Func<RegisterNodeRequest, IServerSyncEndpoint> RegisterNodeFunction { get; set; }

        public event EventHandler<ServerSavingDeltaEventArgs> ServerSavingDelta;
        public event EventHandler<ServerSavedDeltaEventArgs> ServerSavedDelta;
        public event EventHandler<ServerProcessingDeltaEventArgs> ServerProcessingDelta;
        public event EventHandler<ServerProcessedDeltaEventArgs> ServerProcessedDelta;

        public async Task<IEnumerable<IDelta>> GetDeltasAsync(string NodeId, string startIndex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IServerSyncEndpoint Node = GetNode(NodeId);
            if (Node != null)
            {
                return await Node.GetDeltasFromOtherNodes(startIndex,null, cancellationToken).ConfigureAwait(false);
            }

            IEnumerable<IDelta> result = new List<IDelta>();
            return result;
        }
        public async Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string nodeId, string startIndex, string identity, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();
            IServerSyncEndpoint Node = GetNode(nodeId);
            if (Node != null)
            {
                return await Node.GetDeltasFromOtherNodes(startIndex, identity, cancellationToken).ConfigureAwait(false);
            }
            IEnumerable<IDelta> result = new List<IDelta>();
            return result;
        }
        public Task ProcessDeltasAsync(string NodeId, IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IServerSyncEndpoint Node = GetNode(NodeId);
            if (Node != null)
            {
                return Node.ProcessDeltasAsync(deltas, cancellationToken);
            }
            return null;

        }

        private IServerSyncEndpoint GetNode(string NodeId)
        {
            IServerSyncEndpoint syncServerNode = Nodes.FirstOrDefault(node => node.NodeId == NodeId);
            if(syncServerNode==null)
            {
                throw new NodeNotFoundException(NodeId);
            }
            return syncServerNode;
        }

        public Task SaveDeltasAsync(string nodeId, IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IServerSyncEndpoint Node = GetNode(nodeId);

            if (Node != null)
            {
                return Node.SaveDeltasAsync(deltas, cancellationToken);
            }
            return Task.CompletedTask;
        }
        /// <summary>
        ///  Register a node to the server
        /// </summary>
        /// <param name="serverNode"></param>
        /// <returns>True if success otherwise false</returns>
        public bool RegisterNodeAsync(IServerSyncEndpoint serverNode)
        {
            if(this.Nodes.Contains(serverNode)==false)
            {
                this.Nodes.Add(serverNode);
                ISyncServerNodeWithEvents NodeWithEvents = serverNode as ISyncServerNodeWithEvents;
                if (NodeWithEvents != null)
                {
                    RegisterEvents(NodeWithEvents, serverNode);
                  
                }
                
                return true;
            }
            return false;
        }


        private void RegisterEvents(ISyncServerNodeWithEvents syncServerNodeWithEvents, IServerSyncEndpoint syncServerNode)
        {
            if(syncServerNodeWithEvents == null)
            {
                return;
            }
            syncServerNodeWithEvents.NodeSavingDelta += (sender, e) => {
                ServerSavingDelta?.Invoke(sender, new ServerSavingDeltaEventArgs(e));
            };
            syncServerNodeWithEvents.NodeSavedDelta += (sender, e) => {
                ServerSavedDelta?.Invoke(sender, new ServerSavedDeltaEventArgs(e));
            };
            syncServerNodeWithEvents.NodeProcessingDelta += (sender, e) => {
                ServerProcessingDelta?.Invoke(sender, new ServerProcessingDeltaEventArgs(e));
            };
            syncServerNodeWithEvents.NodeProcessedDelta += (sender, e) => {
                ServerProcessedDelta?.Invoke(sender, new ServerProcessedDeltaEventArgs(e));
            };


        }

   
        public bool CreateNodeAsync(RegisterNodeRequest registerNodeRequest)
        {
           return RegisterNodeAsync(this.RegisterNodeFunction(registerNodeRequest));
        }

        public Task<IDelta> GetDeltaAsync(string nodeId, string deltaId, CancellationToken cancellationToken)
        {
            return this.Nodes.Find(node => node.NodeId == nodeId).GetDeltaAsync(deltaId, cancellationToken);
        }
    }
   

}
