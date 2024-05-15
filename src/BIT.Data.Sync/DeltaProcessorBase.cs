
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Base class for delta processors. This class implements the IDeltaProcessor interface and provides a basic implementation.
    /// </summary>
    public abstract class DeltaProcessorBase : IDeltaProcessor
    {
        /// <summary>
        /// The sequence service used by the delta processor.
        /// </summary>
        private readonly ISequenceService sequenceService;

        /// <summary>
        /// Gets the sequence service used by the delta processor.
        /// </summary>
        public ISequenceService SequenceService => sequenceService;

        /// <summary>
        /// Processes a collection of deltas asynchronously.
        /// </summary>
        /// <param name="Deltas">The deltas to process.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public abstract Task ProcessDeltasAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);

        /// <summary>
        /// Initializes a new instance of the DeltaProcessorBase class with the specified sequence service.
        /// </summary>
        /// <param name="sequenceService">The sequence service to use for processing deltas.</param>
        public DeltaProcessorBase(ISequenceService sequenceService)
        {
            this.sequenceService = sequenceService;
        }
    }
}