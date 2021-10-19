
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public abstract class DeltaProcessorBase : IDeltaProcessor
    {
        

        public abstract Task ProcessDeltasAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);

    }
}