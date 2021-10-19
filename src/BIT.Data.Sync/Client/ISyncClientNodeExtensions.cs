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
            var LastDetalIndex = await instance.DeltaStore.GetLastProcessedDeltaAsync(cancellationToken).ConfigureAwait(false);
            return await instance.SyncFrameworkClient.FetchAsync(LastDetalIndex, instance.Identity, cancellationToken).ConfigureAwait(false);
        }
        public static async Task PullAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var Deltas = await instance.FetchAsync(cancellationToken).ConfigureAwait(false);
            if (Deltas.Any())
            {
                await instance.DeltaProcessor.ProcessDeltasAsync(Deltas, cancellationToken).ConfigureAwait(false);
                Guid index = Deltas.Max(d => d.Index);
                await instance.DeltaStore.SetLastProcessedDeltaAsync(index, cancellationToken).ConfigureAwait(false);
            }

        }
        public static async Task PushAsync(this ISyncClientNode instance, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var LastPushedDelta = await instance.DeltaStore.GetLastPushedDeltaAsync(cancellationToken).ConfigureAwait(false);
            var Deltas = await instance.DeltaStore.GetDeltasAsync(LastPushedDelta,cancellationToken).ConfigureAwait(false);
            if (Deltas.Any())
            {
                var Max = Deltas.Max(d => d.Index);
                await instance.SyncFrameworkClient.PushAsync(Deltas, cancellationToken).ConfigureAwait(false);
                await instance.DeltaStore.SetLastPushedDeltaAsync(Max,cancellationToken).ConfigureAwait(false);
            }



        }
    }
}