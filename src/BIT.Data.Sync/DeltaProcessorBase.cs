
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public abstract class DeltaProcessorBase : IDeltaProcessor
    {
        readonly ISequenceService sequenceService;
        public ISequenceService SequenceService => sequenceService;

        public abstract Task ProcessDeltasAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);
        public DeltaProcessorBase(ISequenceService sequenceService)
        {
            this.sequenceService = sequenceService;
        }

    }
}