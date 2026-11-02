using System;
using System.Collections.Generic;

namespace BIT.Data.Sync.Client
{
    /// <summary>
    /// Sends the changes made to some tables to their own server node, through their own delta store.
    /// Changes to tables that no route claims stay on the database's default delta store and client.
    /// </summary>
    public class DeltaRoute
    {
        public DeltaRoute(string name, IDeltaStore deltaStore, ISyncFrameworkClient syncFrameworkClient, IEnumerable<string> tables)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            Name = name;
            DeltaStore = deltaStore ?? throw new ArgumentNullException(nameof(deltaStore));
            SyncFrameworkClient = syncFrameworkClient ?? throw new ArgumentNullException(nameof(syncFrameworkClient));
            Tables = new HashSet<string>(tables ?? throw new ArgumentNullException(nameof(tables)), StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }
        public IDeltaStore DeltaStore { get; }
        public ISyncFrameworkClient SyncFrameworkClient { get; }
        /// <summary>
        /// Table names, optionally schema-qualified ("Employees" or "hr.Employees"), compared ignoring case.
        /// A dot always means schema.table, so tables whose own name contains a dot can't be routed by their bare name.
        /// </summary>
        public ISet<string> Tables { get; }

        public bool Handles(string table, string schema)
        {
            return Tables.Contains(table) || (!string.IsNullOrEmpty(schema) && Tables.Contains($"{schema}.{table}"));
        }

        /// <summary>
        /// Throws when two routes share a name or claim the same table, since the second route would never get its changes.
        /// "Post" (any schema) overlaps "dbo.Post"; "hr.Post" and "pos.Post" don't overlap.
        /// </summary>
        public static IReadOnlyList<DeltaRoute> Validate(IReadOnlyList<DeltaRoute> routes)
        {
            var names = new HashSet<string>();
            var claims = new List<(string Table, string Route)>();
            foreach (var route in routes)
            {
                if (!names.Add(route.Name))
                    throw new InvalidOperationException($"Two delta routes are called '{route.Name}'.");
                foreach (var table in route.Tables)
                {
                    foreach (var claim in claims)
                    {
                        if (claim.Route != route.Name && Overlap(claim.Table, table))
                            throw new InvalidOperationException($"Table '{table}' is claimed by delta routes '{claim.Route}' and '{route.Name}'.");
                    }
                    claims.Add((table, route.Name));
                }
            }
            return routes;
        }

        private static bool Overlap(string a, string b)
        {
            if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
                return true;
            bool aQualified = a.Contains("."), bQualified = b.Contains(".");
            if (aQualified == bQualified)
                return false;
            string bare = aQualified ? b : a, qualified = aQualified ? a : b;
            return qualified.EndsWith("." + bare, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// A client database that splits its changes across several server nodes.
    /// </summary>
    public interface IRoutedClientSyncDatabase : IClientSyncDatabase
    {
        IReadOnlyList<DeltaRoute> Routes { get; }
    }

    /// <summary>
    /// The database seen through one route: same data and identity, the route's delta store and client.
    /// </summary>
    internal class RouteClientSyncDatabase : IClientSyncDatabase
    {
        private readonly IClientSyncDatabase database;
        private readonly DeltaRoute route;

        public RouteClientSyncDatabase(IClientSyncDatabase database, DeltaRoute route)
        {
            this.database = database;
            this.route = route;
        }

        public IDeltaProcessor DeltaProcessor => database.DeltaProcessor;
        public IDeltaStore DeltaStore => route.DeltaStore;
        public ISyncFrameworkClient SyncFrameworkClient => route.SyncFrameworkClient;
        public string Identity => database.Identity;
    }
}
