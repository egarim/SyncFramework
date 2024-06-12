
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
        /// The function will fetch all the Deltas that are greater than <paramref name="startIndex"/>
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="identity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken);
        
        Task ProcessDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
        /// <summary>
        /// The function will fetch all the deltas that are greater than current <paramref name="deltaId"/>
        /// </summary>
        /// <param name="deltaId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken);

    }
  

}
