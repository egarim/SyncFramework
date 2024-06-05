
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public class SyncServerNodeList:List<ISyncServerNode>
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
        public SyncServer(IEnumerable<ISyncServerNode> nodes, Func<RegisterNodeRequest, ISyncServerNode> registerNodeRequest,IInfoServer infoServer)
        {
            Nodes.AddRange(nodes);
            foreach (ISyncServerNode syncServerNode in Nodes)
            {
                RegisterDeltaProcessorEvents(syncServerNode as IDeltaProcessorWithEvents, syncServerNode);
                RegisterDeltaStoreEvents(syncServerNode as IDeltaStoreWithEvents, syncServerNode);
            }
            RegisterNodeFunction = registerNodeRequest;
        }
        public SyncServer(params ISyncServerNode[] Nodes) : this(Nodes, null, null)
        {

        }
        public SyncServer(SyncServerNodeList Nodes) : this(Nodes.ToArray(), null, null)
        {

        }
        protected SyncServer()
        {
         
        }
   
        public List<ISyncServerNode> Nodes { get; } = new List<ISyncServerNode>();
        public Func<RegisterNodeRequest, ISyncServerNode> RegisterNodeFunction { get; set; }

        public event EventHandler<SyncServerSavingDeltaEventArgs> SavingDelta;
        public event EventHandler<SyncServerSavedDeltaEventArgs> SavedDelta;
        public event EventHandler<SyncServerProcessingDeltaEventArgs> ProcessingDelta;
        public event EventHandler<SyncServerProcessDeltaBaseEventArgs> ProcessedDelta;

        public async Task<IEnumerable<IDelta>> GetDeltasAsync(string NodeId, string startIndex, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ISyncServerNode Node = GetNode(NodeId);
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
            ISyncServerNode Node = GetNode(nodeId);
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
                IDeltaStoreWithEvents deltaStoreWithEvents = serverNode as IDeltaStoreWithEvents;
                if (deltaStoreWithEvents != null)
                {
                    RegisterDeltaStoreEvents(deltaStoreWithEvents, serverNode);
                }
                IDeltaProcessorWithEvents deltaProcessorWithEvents = serverNode as IDeltaProcessorWithEvents;
                if (deltaProcessorWithEvents != null)
                {
                    RegisterDeltaProcessorEvents(deltaProcessorWithEvents, serverNode);
                }
                return true;
            }
            return false;
        }

        private void RegisterDeltaProcessorEvents(IDeltaProcessorWithEvents deltaProcessorWithEvents,ISyncServerNode syncServerNode)
        {
            if(deltaProcessorWithEvents==null)
            {
                return;
            }
            deltaProcessorWithEvents.ProcessingDelta += (sender, e) => {
                ProcessingDelta?.Invoke(sender, new SyncServerProcessingDeltaEventArgs(e.Delta, syncServerNode));
            };
            deltaProcessorWithEvents.ProcessedDelta += (sender, e) => {
                ProcessedDelta?.Invoke(sender, new SyncServerProcessDeltaBaseEventArgs(e.Delta, syncServerNode));
            };  

        }

        private void RegisterDeltaStoreEvents(IDeltaStoreWithEvents deltaStoreWithEvents, ISyncServerNode syncServerNode)
        {
            if(deltaStoreWithEvents==null)
            {
                return;
            }   
            deltaStoreWithEvents.SavingDelta += (sender, e) => {
                SavingDelta?.Invoke(sender, new SyncServerSavingDeltaEventArgs(e.Delta, syncServerNode));
            };
            deltaStoreWithEvents.SavedDelta += (sender, e) => {
                SavedDelta?.Invoke(sender, new SyncServerSavedDeltaEventArgs(e.Delta, syncServerNode));
            };

           
        }

   
        public bool RegisterNodeAsync(RegisterNodeRequest registerNodeRequest)
        {
           return RegisterNodeAsync(this.RegisterNodeFunction(registerNodeRequest));
        }
    }
   

}
