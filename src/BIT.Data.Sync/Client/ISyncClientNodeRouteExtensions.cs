using BIT.Data.Sync.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Push and pull for one <see cref="DeltaRoute"/>, or for the default route and then every route.
    /// </summary>
    public static class ISyncClientNodeRouteExtensions
    {
        /// <summary>
        /// The database seen through the route called <paramref name="routeName"/>, ready for the usual
        /// PushAsync / PullAsync / FetchAsync / RestoreAsync.
        /// </summary>
        public static IClientSyncDatabase ForRoute(this IClientSyncDatabase instance, string routeName)
        {
            var route = (instance as IRoutedClientSyncDatabase)?.Routes?.FirstOrDefault(r => r.Name == routeName);
            if (route == null)
                throw new ArgumentException($"No delta route called '{routeName}' is registered.", nameof(routeName));
            return new RouteClientSyncDatabase(instance, route);
        }

        public static Task<PushOperationResponse> PushAsync(this IClientSyncDatabase instance, string routeName, CancellationToken cancellationToken = default)
        {
            return instance.ForRoute(routeName).PushAsync(cancellationToken);
        }

        public static Task<FetchOperationResponse> PullAsync(this IClientSyncDatabase instance, string routeName, CancellationToken cancellationToken = default)
        {
            return instance.ForRoute(routeName).PullAsync(cancellationToken);
        }

        /// <summary>
        /// Pushes the default route, then each route in the order they were registered.
        /// </summary>
        public static async Task<IReadOnlyList<PushOperationResponse>> PushAllRoutesAsync(this IClientSyncDatabase instance, CancellationToken cancellationToken = default)
        {
            var responses = new List<PushOperationResponse> { await instance.PushAsync(cancellationToken).ConfigureAwait(false) };
            foreach (var route in RoutesOf(instance))
                responses.Add(await new RouteClientSyncDatabase(instance, route).PushAsync(cancellationToken).ConfigureAwait(false));
            return responses;
        }

        /// <summary>
        /// Pulls the default route, then each route in the order they were registered, so parent rows
        /// kept on the default route arrive before the rows that reference them.
        /// </summary>
        public static async Task<IReadOnlyList<FetchOperationResponse>> PullAllRoutesAsync(this IClientSyncDatabase instance, CancellationToken cancellationToken = default)
        {
            var responses = new List<FetchOperationResponse> { await instance.PullAsync(cancellationToken).ConfigureAwait(false) };
            foreach (var route in RoutesOf(instance))
                responses.Add(await new RouteClientSyncDatabase(instance, route).PullAsync(cancellationToken).ConfigureAwait(false));
            return responses;
        }

        private static IEnumerable<DeltaRoute> RoutesOf(IClientSyncDatabase instance)
        {
            return (instance as IRoutedClientSyncDatabase)?.Routes ?? Enumerable.Empty<DeltaRoute>();
        }
    }
}
