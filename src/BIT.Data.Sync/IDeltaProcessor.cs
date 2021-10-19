using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public interface IDeltaProcessor
    {
        /// <summary>
        /// Extracts the content of an IEnumerable of deltas and process it on the current data object
        /// </summary>
        /// <param name="deltas">an IEnumerable of deltas</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>An empty task</returns>
        Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    }
}