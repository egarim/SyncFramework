using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.Imp;
using BIT.EfCore.Sync;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Tests
{
    /// <summary>
    /// SQLite only, so it runs without the database servers <see cref="AllDatabaseTests"/> needs.
    /// </summary>
    public class DeltaTrackingTests
    {
        private string folder;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "SyncFrameworkDeltaTracking_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void TearDown()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }

        /// <summary>
        /// Saves for real, but only after yielding, so a caller that doesn't wait for the save sees no delta yet.
        /// </summary>
        private class SlowDeltaStore : MemoryDeltaStore
        {
            public override async Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
            {
                await Task.Delay(200, cancellationToken);
                await base.SaveDeltasAsync(deltas, cancellationToken);
            }
        }

        private TestSyncFrameworkDbContext NewContext(IDeltaStore deltaStore = null)
        {
            var services = new ServiceCollection();
            services.AddSyncFrameworkForSQLite($"Data Source={Path.Combine(folder, "deltas.db")}",
                new HttpClient { BaseAddress = new Uri("http://localhost/sync/") }, "Node", "Client",
                new SqliteDeltaGenerator());
            services.AddSingleton(typeof(ISequencePrefixStrategy), new YearSequencePrefixStrategy());
            services.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            if (deltaStore != null)
                services.AddSingleton(deltaStore);

            var options = new DbContextOptionsBuilder().UseSqlite($"Data Source={Path.Combine(folder, "data.db")}").Options;
            var context = new TestSyncFrameworkDbContext(options, services.BuildServiceProvider());
            context.Database.EnsureCreated();
            return context;
        }

        private static Task<int> CountDeltas(TestSyncFrameworkDbContext context)
            => context.DeltaStore.GetDeltaCountAsync(string.Empty, context.Identity, default);

        [Test]
        public async Task SaveChangesAsyncRecordsDeltasOnlyWhileTrackingIsOn()
        {
            using var context = NewContext();
            Assert.IsTrue(context.EnableDeltaTracking, "tracking is on by default");

            context.Add(new Blog { Name = "tracked" });
            await context.SaveChangesAsync();
            Assert.AreEqual(1, await CountDeltas(context));

            context.EnableDeltaTracking = false;
            context.Add(new Blog { Name = "not tracked" });
            await context.SaveChangesAsync();
            Assert.AreEqual(1, await CountDeltas(context));
            Assert.AreEqual(2, context.Blogs.Count(), "the change itself is still saved");

            context.EnableDeltaTracking = true;
            context.Add(new Blog { Name = "tracked again" });
            await context.SaveChangesAsync();
            Assert.AreEqual(2, await CountDeltas(context));
        }

        [Test]
        public async Task SyncSaveChangesWritesTheDeltaBeforeReturning()
        {
            using var context = NewContext(new SlowDeltaStore());

            context.Add(new Blog { Name = "sync" });
            context.SaveChanges();
            Assert.AreEqual(1, await CountDeltas(context));

            context.EnableDeltaTracking = false;
            context.Add(new Blog { Name = "sync, not tracked" });
            context.SaveChanges();
            Assert.AreEqual(1, await CountDeltas(context));
        }
    }
}
