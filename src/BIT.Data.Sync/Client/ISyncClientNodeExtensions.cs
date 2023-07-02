using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Client
{
    public static class ISyncClientNodeExtensions
    {

        public static async Task<List<Delta>> FetchAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var statusExists = await instance.DeltaStore.CanRestoreDatabaseAsync(instance.Identity, cancellationToken);
            var LastDeltaIndex = await instance.DeltaStore.GetLastProcessedDeltaAsync(instance.Identity, cancellationToken).ConfigureAwait(false);
            //TODO this might not be needed 
            if (!statusExists)
                return await instance.SyncFrameworkClient.FetchAsync(LastDeltaIndex, string.Empty, cancellationToken).ConfigureAwait(false);

            return await instance.SyncFrameworkClient.FetchAsync(LastDeltaIndex, instance.Identity, cancellationToken).ConfigureAwait(false);
        }
        public static async Task PullAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {            
            cancellationToken.ThrowIfCancellationRequested();
            var Deltas = await instance.FetchAsync(cancellationToken).ConfigureAwait(false);
            if (Deltas.Any())
            {
                await instance.DeltaProcessor.ProcessDeltasAsync(Deltas, cancellationToken).ConfigureAwait(false);
                string index = Deltas.Max(d => d.Index);
                await instance.DeltaStore.SetLastProcessedDeltaAsync(index, instance.Identity, cancellationToken).ConfigureAwait(false);
            }

        }
        public static async Task PushAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var LastPushedDelta = await instance.DeltaStore.GetLastPushedDeltaAsync(instance.Identity, cancellationToken).ConfigureAwait(false);
            var Deltas = await instance.DeltaStore.GetDeltasByIdentityAsync( LastPushedDelta, instance.Identity, cancellationToken).ConfigureAwait(false);
            if (Deltas.Any())
            {
                var Max = Deltas.Max(d => d.Index);
                await instance.SyncFrameworkClient.PushAsync(Deltas, cancellationToken).ConfigureAwait(false);
                await instance.DeltaStore.SetLastPushedDeltaAsync(Max, instance.Identity, cancellationToken).ConfigureAwait(false);
            }



        }
    }
}