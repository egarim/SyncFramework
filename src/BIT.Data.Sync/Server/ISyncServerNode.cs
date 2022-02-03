
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerNode
    {
        string NodeId { get; set; }
        Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
        /// <summary>
        /// The function will fetch all the delats that are greater than <paramref name="startindex"/>
        /// </summary>
        /// <param name="startindex"></param>
        /// <param name="identity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<IDelta>> GetDeltasAsync(Guid startindex, string identity, CancellationToken cancellationToken);
        
        Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    }

    
}
