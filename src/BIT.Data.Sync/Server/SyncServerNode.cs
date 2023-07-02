
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public class SyncServerNode : ISyncServerNode
    {
        IDeltaStore deltaStore;
        IDeltaProcessor deltaProcessor;
        public SyncServerNode(IDeltaStore deltaStore, IDeltaProcessor deltaProcessor,string nodeId)
        {
            this.deltaStore = deltaStore;
            this.deltaProcessor = deltaProcessor;
            this.NodeId = nodeId;
        }

        public string NodeId { get; set; }

        public virtual Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, string identity, CancellationToken cancellationToken)
        {
            if(string.IsNullOrEmpty(identity))
                return this.deltaStore?.GetDeltasAsync(startIndex, cancellationToken);
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
