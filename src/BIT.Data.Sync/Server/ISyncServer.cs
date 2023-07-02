
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServer
    {
        IEnumerable<ISyncServerNode> Nodes { get; }
        /// <summary>
        /// The function will fetch all the deltas that are greater than current <paramref name="startIndex"/>
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="startIndex"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<IDelta>> GetDeltasAsync(string nodeId, string startIndex, CancellationToken cancellationToken);
        /// <summary>
        /// The function will fetch all other node deltas that are greater than current <paramref name="startIndex"/>
        /// </summary>
        /// <param name="nodeId"></param>
        /// <param name="startIndex"></param>
        /// <param name="identity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string nodeId, string startIndex, string identity, CancellationToken cancellationToken);
        Task ProcessDeltasAsync(string Name, IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
        Task SaveDeltasAsync(string name, IEnumerable<IDelta> deltas, CancellationToken cancellationToken);
    }
}
