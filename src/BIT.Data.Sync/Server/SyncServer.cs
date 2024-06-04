
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public class SynServerNodeList:List<ISyncServerNode>
    {
        
        public SynServerNodeList()
        {
            
        }
    }
    public class SyncServer : ISyncServer
    {
        public SyncServer(IEnumerable<ISyncServerNode> nodes, Func<RegisterNodeRequest, ISyncServerNode> registerNodeRequest)
        {
            Nodes.AddRange(nodes);
            RegisterNodeFunction = registerNodeRequest;
        }
        public SyncServer()
        {
            Nodes = new List<ISyncServerNode>();
        }
        public SyncServer(params ISyncServerNode[] Nodes)
        {
            this.Nodes.AddRange(Nodes);
        }
        public SyncServer(SynServerNodeList Nodes)
        {
            this.Nodes = Nodes;
        }
        public List<ISyncServerNode> Nodes { get; } = new List<ISyncServerNode>();
        public Func<RegisterNodeRequest, ISyncServerNode> RegisterNodeFunction { get; set; }

        public async Task<IEnumerable<IDelta>> GetDeltasAsync(string NodeId, string startIndex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISyncServerNode Node = GetNode(NodeId);
            if (Node != null)
            {
                return await Node.GetDeltasAsync(startIndex,null, cancellationToken).ConfigureAwait(false);
            }

            IEnumerable<IDelta> result = new List<IDelta>();
            return result;
        }
        public async Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string nodeId, string startIndex, string identity, CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();
            ISyncServerNode Node = GetNode(nodeId);
            if (Node != null)
            {
                return await Node.GetDeltasAsync(startIndex, identity, cancellationToken).ConfigureAwait(false);
            }
            IEnumerable<IDelta> result = new List<IDelta>();
            return result;
        }
        public Task ProcessDeltasAsync(string NodeId, IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISyncServerNode Node = GetNode(NodeId);
            if (Node != null)
            {
                return Node.ProcessDeltasAsync(deltas, cancellationToken);
            }
            return null;

        }

        private ISyncServerNode GetNode(string NodeId)
        {
            ISyncServerNode syncServerNode = Nodes.FirstOrDefault(node => node.NodeId == NodeId);
            if(syncServerNode==null)
            {
                throw new NodeNotFoundException(NodeId);
            }
            return syncServerNode;
        }

        public Task SaveDeltasAsync(string nodeId, IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ISyncServerNode Node = GetNode(nodeId);

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
        public bool RegisterNodeAsync(ISyncServerNode serverNode)
        {
            if(this.Nodes.Contains(serverNode)==false)
            {
                this.Nodes.Add(serverNode);
                return true;
            }
            return false;
        }

        public bool RegisterNodeAsync(RegisterNodeRequest registerNodeRequest)
        {
           return RegisterNodeAsync(this.RegisterNodeFunction(registerNodeRequest));
        }
    }
   

}
