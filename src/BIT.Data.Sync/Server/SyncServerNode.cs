
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{

    public class SyncServerNode : IServerSyncEndpoint, ISyncServerNodeWithEvents
    {
        IDeltaStore deltaStore;
        IDeltaProcessor deltaProcessor;
        public SyncServerNode(IDeltaStore deltaStore, IDeltaProcessor deltaProcessor,string ServerNodeId)
        {
            this.deltaStore = deltaStore;
            IDeltaStoreWithEvents deltaStoreWithEvents = deltaStore as IDeltaStoreWithEvents;
            if (deltaStoreWithEvents != null)
            {
                deltaStoreWithEvents.SavingDelta += (sender, e) => NodeSavingDelta?.Invoke(sender,new NodeSavingDeltaEventArgs(this,e));
                deltaStoreWithEvents.SavedDelta += (sender, e) => NodeSavedDelta?.Invoke(sender, new NodeSavedDeltaEventArgs(this, e));
            }      
            this.deltaProcessor = deltaProcessor;
            IDeltaProcessorWithEvents deltaProcessorWithEvents = deltaProcessor as IDeltaProcessorWithEvents;
            if (deltaProcessorWithEvents != null)
            {
                deltaProcessorWithEvents.ProcessingDelta += (sender, e) => NodeProcessingDelta?.Invoke(sender, new NodeProcessingDeltaEventArgs(this, e));
                deltaProcessorWithEvents.ProcessedDelta += (sender, e) => NodeProcessedDelta?.Invoke(sender, new NodeProcessedDeltaEventArgs(this,e));
            }
            this.NodeId = ServerNodeId;
        }

        public string NodeId { get; set; }

        public IDeltaStore DeltaStore => deltaStore;

        public event EventHandler<NodeSavingDeltaEventArgs> NodeSavingDelta;
        public event EventHandler<NodeSavedDeltaEventArgs> NodeSavedDelta;
        public event EventHandler<NodeProcessingDeltaEventArgs> NodeProcessingDelta;
        public event EventHandler<NodeProcessedDeltaEventArgs> NodeProcessedDelta;

        public Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken)
        {
           return this.deltaStore?.GetDeltaAsync(deltaId, cancellationToken);
        }

        public virtual Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(identity))
                throw new ArgumentNullException(nameof(identity));
            if(string.IsNullOrEmpty(startIndex))
                throw new ArgumentNullException(nameof(startIndex));

            return this.deltaStore?.GetDeltasFromOtherClients(startIndex, identity, cancellationToken);
        }

        public virtual Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            return this.deltaProcessor?.ProcessDeltasAsync(deltas, cancellationToken);
        }

        public virtual Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken)
        {
            return this.deltaStore?.SaveDeltasAsync(deltas, cancellationToken);
        }
    }
}
