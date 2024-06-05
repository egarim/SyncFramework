
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public class SyncServerNode : ISyncServerNode,IDeltaStoreWithEvents,IDeltaProcessorWithEvents
    {
        IDeltaStore deltaStore;
        IDeltaProcessor deltaProcessor;
        public SyncServerNode(IDeltaStore deltaStore, IDeltaProcessor deltaProcessor,string nodeId)
        {
            this.deltaStore = deltaStore;
            IDeltaStoreWithEvents deltaStoreWithEvents = deltaStore as IDeltaStoreWithEvents;
            if (deltaStoreWithEvents != null)
            {
                deltaStoreWithEvents.SavingDelta += (sender, e) => SavingDelta?.Invoke(sender, e);
                deltaStoreWithEvents.SavedDelta += (sender, e) => SavedDelta?.Invoke(sender, e);
            }      
            this.deltaProcessor = deltaProcessor;
            IDeltaProcessorWithEvents deltaProcessorWithEvents = deltaProcessor as IDeltaProcessorWithEvents;
            if (deltaProcessorWithEvents != null)
            {
                deltaProcessorWithEvents.ProcessingDelta += (sender, e) => ProcessingDelta?.Invoke(sender, e);
                deltaProcessorWithEvents.ProcessedDelta += (sender, e) => ProcessedDelta?.Invoke(sender, e);
            }
            this.NodeId = nodeId;
        }

        public string NodeId { get; set; }

        public event EventHandler<SavingDeltaEventArgs> SavingDelta;
        public event EventHandler<SavedDeltaEventArgs> SavedDelta;
        public event EventHandler<ProcessingDeltaEventArgs> ProcessingDelta;
        public event EventHandler<ProcessDeltaBaseEventArgs> ProcessedDelta;


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
