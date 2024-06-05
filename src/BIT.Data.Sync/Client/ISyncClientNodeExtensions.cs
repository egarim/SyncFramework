using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ISyncClientNodeExtensions
    {

        public static async Task<FetchOperationResponse> FetchAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var LastDeltaIndex = await instance.DeltaStore.GetLastProcessedDeltaAsync(instance.Identity, cancellationToken).ConfigureAwait(false);
            return await instance.SyncFrameworkClient.FetchAsync(LastDeltaIndex, instance.Identity, cancellationToken).ConfigureAwait(false);
        }
        public static async Task<FetchOperationResponse> PullAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {            
            cancellationToken.ThrowIfCancellationRequested();
            var Response = await instance.FetchAsync(cancellationToken).ConfigureAwait(false);
            if (Response.Deltas.Any())
            {
                await instance.DeltaProcessor.ProcessDeltasAsync(Response.Deltas, cancellationToken).ConfigureAwait(false);
                string index = Response.Deltas.Max(d => d.Index);
                await instance.DeltaStore.SetLastProcessedDeltaAsync(index, instance.Identity, cancellationToken).ConfigureAwait(false);
            }
            return Response;

        }
        public static async Task<PushOperationResponse> PushAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            PushOperationResponse Response = null;
            cancellationToken.ThrowIfCancellationRequested();
            var LastPushedDelta = await instance.DeltaStore.GetLastPushedDeltaAsync(instance.Identity, cancellationToken).ConfigureAwait(false);
            var Deltas = await instance.DeltaStore.GetDeltasByIdentityAsync( LastPushedDelta, instance.Identity, cancellationToken).ConfigureAwait(false);
            if (Deltas.Any())
            {
                var Max = Deltas.Max(d => d.Index);
                Response = await instance.SyncFrameworkClient.PushAsync(Deltas, cancellationToken).ConfigureAwait(false);
                if(Response.Success)
                    await instance.DeltaStore.SetLastPushedDeltaAsync(Max, instance.Identity, cancellationToken).ConfigureAwait(false);
            }
            return Response;



        }
    }
}