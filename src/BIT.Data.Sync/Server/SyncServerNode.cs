
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{

    public class SyncServerNode : ISyncServerNode, ISyncServerNodeWithEvents
    {
        IDeltaStore deltaStore;
        IDeltaProcessor deltaProcessor;
        public SyncServerNode(IDeltaStore deltaStore, IDeltaProcessor deltaProcessor,string nodeId)
        {
            this.deltaStore = deltaStore;
            IDeltaStoreWithEvents deltaStoreWithEvents = deltaStore as IDeltaStoreWithEvents;
            if (deltaStoreWithEvents != null)
            {
                deltaStoreWithEvents.SavingDelta += (sender, e) => SavingDelta?.Invoke(sender,new NodeSavingDeltaEventArgs(e.Delta,this,e));
                deltaStoreWithEvents.SavedDelta += (sender, e) => SavedDelta?.Invoke(sender, new NodeSavedDeltaEventArgs(e.Delta, this, e));
            }      
            this.deltaProcessor = deltaProcessor;
            IDeltaProcessorWithEvents deltaProcessorWithEvents = deltaProcessor as IDeltaProcessorWithEvents;
            if (deltaProcessorWithEvents != null)
            {
                deltaProcessorWithEvents.ProcessingDelta += (sender, e) => ProcessingDelta?.Invoke(sender, new NodeProcessingDeltaEventArgs(e.Delta,this, e));
                deltaProcessorWithEvents.ProcessedDelta += (sender, e) => ProcessedDelta?.Invoke(sender, new NodeProcessedDeltaEventArgs(e.Delta,this,e));
            }
            this.NodeId = nodeId;
        }

        public string NodeId { get; set; }

        public event EventHandler<NodeSavingDeltaEventArgs> SavingDelta;
        public event EventHandler<NodeSavedDeltaEventArgs> SavedDelta;
        public event EventHandler<NodeProcessingDeltaEventArgs> ProcessingDelta;
        public event EventHandler<NodeProcessedDeltaEventArgs> ProcessedDelta;

        public virtual Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(identity))
                throw new ArgumentNullException(nameof(identity));
            if(string.IsNullOrEmpty(startIndex))
                throw new ArgumentNullException(nameof(startIndex));

            return this.deltaStore?.GetDeltasFromOtherNodes(startIndex, identity, cancellationToken);
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
