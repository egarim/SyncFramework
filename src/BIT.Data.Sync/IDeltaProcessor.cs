using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a delta processor, which processes a collection of deltas.
    /// </summary>
    public interface IDeltaProcessor
    {
        /// <summary>
        /// Extracts the content of an IEnumerable of deltas and processes it on the current data object.
        /// </summary>
        /// <param name="deltas">An IEnumerable of deltas.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An empty task that represents the asynchronous operation.</returns>
        Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the sequence service used by the delta processor.
        /// </summary>
        ISequenceService SequenceService { get; }

    
    }
}