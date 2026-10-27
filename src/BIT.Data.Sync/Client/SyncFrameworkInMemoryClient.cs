using BIT.Data.Sync.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Client
{
    /// <summary>
    /// An <see cref="ISyncFrameworkClient"/> that talks to an <see cref="ISyncFrameworkServer"/> in the same process, with
    /// no HTTP in between: for tests, simulations, and an app that hosts its own server. It answers exactly as the HTTP
    /// controller (<c>SyncControllerBase</c>) does for Push, Fetch and HandShake, and copies every delta on the way in and
    /// out, as serialization over HTTP would: the server re-indexes the deltas it saves, and without the copy it would
    /// renumber the client's own stored deltas under it.
    /// </summary>
    public class SyncFrameworkInMemoryClient : ISyncFrameworkClient
    {
        private readonly ISyncFrameworkServer _server;

        public SyncFrameworkInMemoryClient(ISyncFrameworkServer server, string serverNodeId)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            ServerNodeId = serverNodeId ?? throw new ArgumentNullException(nameof(serverNodeId));
        }

        /// <summary>The node on the server this client pushes to and fetches from (the HTTP client's "NodeId" header).</summary>
        public string ServerNodeId { get; }

        public async Task<PushOperationResponse> PushAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken)
        {
            var response = new PushOperationResponse { ServerNodeId = ServerNodeId };
            try
            {
                var copies = Deltas.Select(d => (IDelta)new Delta(d)).ToList();
                response.ClientNodeId = copies.FirstOrDefault()?.Identity;
                await _server.SaveDeltasAsync(ServerNodeId, copies, cancellationToken).ConfigureAwait(false);
                response.ProcessedDeltasIds.AddRange(copies.Select(d => d.DeltaId));
                response.Success = true;
                response.Message = $"Push to node:{ServerNodeId}{Environment.NewLine}Deltas Received:{copies.Count}{Environment.NewLine}Identity:{response.ClientNodeId}";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public async Task<FetchOperationResponse> FetchAsync(string startIndex, string identity, CancellationToken cancellationToken)
        {
            var response = new FetchOperationResponse { ServerNodeId = ServerNodeId, ClientNodeId = identity };
            try
            {
                startIndex ??= "";
                var deltas = string.IsNullOrEmpty(identity)
                    ? await _server.GetDeltasAsync(ServerNodeId, startIndex, cancellationToken).ConfigureAwait(false)
                    : await _server.GetDeltasFromOtherNodes(ServerNodeId, startIndex, identity, cancellationToken).ConfigureAwait(false);
                response.Deltas.AddRange(deltas.Select(d => new Delta(d)));
                response.Success = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            return response;
        }

        public Task<FetchOperationResponse> HandShake(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _server.HandShake();
            return Task.FromResult(new FetchOperationResponse { Success = true, ServerNodeId = ServerNodeId });
        }
    }
}
