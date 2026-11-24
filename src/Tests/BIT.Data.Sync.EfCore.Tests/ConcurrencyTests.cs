using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Model;
using BIT.Data.Sync.Imp;
using BIT.EfCore.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Tests
{
    /// <summary>
    /// Concurrent use of the EF delta store and sequence service (issue #34). SQLite and in-memory databases only.
    /// </summary>
    public class ConcurrencyTests
    {
        public enum Database { SqliteFile, SqliteSharedMemory }

        private string folder;
        // Shared in-memory SQLite databases live only while a connection to them is open.
        private readonly List<SqliteConnection> keepAlive = new List<SqliteConnection>();

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "SyncFrameworkConcurrency_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
        }

        [TearDown]
        public void TearDown()
        {
            keepAlive.ForEach(c => c.Dispose());
            keepAlive.Clear();
            SqliteConnection.ClearAllPools();
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }

        private string ConnectionString(Database database, string name)
        {
            if (database == Database.SqliteFile)
                return $"Data Source={Path.Combine(folder, name + ".db")}";

            string connectionString = $"Data Source={name}_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var connection = new SqliteConnection(connectionString);
            connection.Open();
            keepAlive.Add(connection);
            return connectionString;
        }

        private static IServiceProvider NewServices(string deltasConnectionString)
        {
            var services = new ServiceCollection();
            services.AddSyncFrameworkForSQLite(deltasConnectionString,
                new HttpClient { BaseAddress = new Uri("http://localhost/sync/") }, "Node", "Client",
                new SqliteDeltaGenerator());
            services.AddSingleton(typeof(ISequencePrefixStrategy), new YearSequencePrefixStrategy());
            services.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            return services.BuildServiceProvider();
        }

        private static TestSyncFrameworkDbContext NewContext(string dataConnectionString, IServiceProvider services)
            => new TestSyncFrameworkDbContext(new DbContextOptionsBuilder().UseSqlite(dataConnectionString).Options, services);

        private static void AssertDistinct(IEnumerable<IDelta> deltas, int expected)
        {
            var indexes = deltas.Select(d => d.Index).ToList();
            Assert.AreEqual(expected, indexes.Count, "delta count");
            Assert.AreEqual(expected, indexes.Distinct().Count(), "every delta has its own index: " + string.Join(", ", indexes.OrderBy(i => i)));
        }

        private static List<long> Numbers(IEnumerable<string> indexes, string prefix)
            => indexes.Select(i => { StringAssert.StartsWith(prefix, i); return long.Parse(i.Substring(prefix.Length)); }).ToList();

        // 20 tasks, each with its own SyncFramework DbContext on the same database, call SaveChangesAsync at the same time.
        [TestCase(Database.SqliteFile, true)]
        [TestCase(Database.SqliteFile, false)]
        [TestCase(Database.SqliteSharedMemory, true)]
        [TestCase(Database.SqliteSharedMemory, false)]
        public async Task ParallelSaveChangesAsyncRecordsOneDeltaEachWithDistinctIndexes(Database database, bool sharedServices)
        {
            const int count = 20;
            string data = ConnectionString(database, "data");
            string deltas = ConnectionString(database, "deltas");
            var shared = NewServices(deltas);
            using (var setup = NewContext(data, shared))
                setup.Database.EnsureCreated();
            var store = shared.GetRequiredService<IDeltaStore>();
            int before = (await store.GetDeltasAsync(string.Empty)).Count();

            // Shared: one provider, so one singleton store and sequence service, like an ASP.NET Core app.
            // Not shared: a store and sequence service per context, so only the database orders them.
            using var start = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(0, count).Select(i => Task.Run(async () =>
            {
                start.Wait();
                using var context = NewContext(data, sharedServices ? shared : NewServices(deltas));
                context.Add(new Blog { Name = "blog " + i });
                await context.SaveChangesAsync();
            })).ToList();
            start.Set();
            await Task.WhenAll(tasks);

            var saved = (await store.GetDeltasAsync(string.Empty)).ToList();
            Assert.AreEqual(0, before);
            AssertDistinct(saved, count);
            using var check = NewContext(data, shared);
            Assert.AreEqual(count, check.Blogs.Count());
        }

        // One store resolved from DI (a singleton) gets 20 concurrent saves and concurrent reads.
        [TestCase(Database.SqliteFile)]
        [TestCase(Database.SqliteSharedMemory)]
        public async Task SingletonStoreServesConcurrentSavesAndReads(Database database)
        {
            const int count = 20;
            var services = NewServices(ConnectionString(database, "deltas"));
            var store = services.GetRequiredService<IDeltaStore>();
            Assert.AreSame(store, services.GetRequiredService<IDeltaStore>(), "the store is a singleton");
            Assert.IsInstanceOf<EfDeltaStore>(store);

            using var start = new ManualResetEventSlim(false);
            var saves = Enumerable.Range(0, count).Select(i => Task.Run(async () =>
            {
                start.Wait();
                await store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", "delta " + i) });
            }));
            var reads = Enumerable.Range(0, count).Select(i => Task.Run(async () =>
            {
                start.Wait();
                await store.GetDeltasAsync(string.Empty);
                await store.GetDeltaCountAsync(string.Empty, "Client");
            }));
            var all = saves.Concat(reads).ToList();
            start.Set();
            // WhenAll rethrows the first failure, for example "A second operation was started on this context".
            await Task.WhenAll(all);

            AssertDistinct(await store.GetDeltasAsync(string.Empty), count);
        }

        [Test]
        public async Task SingletonStoreOnEfInMemoryServesConcurrentSavesAndReads()
        {
            const int count = 20;
            var options = new DbContextOptionsBuilder<DeltaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var store = new EfDeltaStore(() => new DeltaDbContext(options),
                new EfSequenceService(new YearSequencePrefixStrategy(), () => new DeltaDbContext(options)));

            var calls = Enumerable.Range(0, count).SelectMany(i => new[]
            {
                Task.Run(() => store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", "delta " + i) })),
                Task.Run(() => (Task)store.GetDeltasAsync(string.Empty)),
            }).ToList();
            await Task.WhenAll(calls);

            AssertDistinct(await store.GetDeltasAsync(string.Empty), count);
        }

        [Test]
        public void StoreAndSequenceServiceKeepNoContextForOperations()
        {
            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var sequenceFields = typeof(EfSequenceService).GetFields(instance).Where(f => typeof(DbContext).IsAssignableFrom(f.FieldType));
            CollectionAssert.IsEmpty(sequenceFields.Select(f => f.Name), "EfSequenceService holds no DbContext");

            // EfDeltaStore keeps the public DeltaDbContext property for existing callers; it must be its only DbContext field.
            var storeFields = typeof(EfDeltaStore).GetFields(instance).Where(f => typeof(DbContext).IsAssignableFrom(f.FieldType));
            CollectionAssert.AreEqual(new[] { "<DeltaDbContext>k__BackingField" }, storeFields.Select(f => f.Name));
        }

        [Test]
        public async Task TheContextPassedToTheConstructorIsNotUsedForOperations()
        {
            var options = new DbContextOptionsBuilder<DeltaDbContext>().UseSqlite(ConnectionString(Database.SqliteFile, "deltas")).Options;
            var template = new DeltaDbContext(options);
            var store = new EfDeltaStore(template, new EfSequenceService(new YearSequencePrefixStrategy(), template));
            template.Dispose();

            await store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", "after dispose") });
            await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() => store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", i) }))));

            AssertDistinct(await store.GetDeltasAsync(string.Empty), 11);
        }

        // Marks the calls whose context the factory disposes before handing it out.
        private static readonly AsyncLocal<bool> disposeMyContext = new AsyncLocal<bool>();

        [Test]
        public async Task EveryOperationGetsItsOwnContextAndDisposesIt()
        {
            var options = new DbContextOptionsBuilder<DeltaDbContext>().UseSqlite(ConnectionString(Database.SqliteFile, "deltas")).Options;
            var created = new ConcurrentBag<DeltaDbContext>();
            DeltaDbContext Factory()
            {
                var context = new DeltaDbContext(options);
                created.Add(context);
                if (disposeMyContext.Value)
                    context.Dispose();
                return context;
            }
            var store = new EfDeltaStore(Factory, new EfSequenceService(new YearSequencePrefixStrategy(), () => new DeltaDbContext(options)));
            created.Clear();

            // Concurrent calls each create their own context, and none is left undisposed.
            const int count = 10;
            await Task.WhenAll(Enumerable.Range(0, count).SelectMany(i => new[]
            {
                Task.Run(() => store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", i) })),
                Task.Run(() => (Task)store.GetDeltasAsync(string.Empty)),
            }));
            Assert.AreEqual(2 * count, created.Count);
            Assert.AreEqual(2 * count, created.Distinct().Count(), "no context is shared between calls");
            foreach (var context in created)
                Assert.Throws<ObjectDisposedException>(() => context.Deltas.Count(), "each call disposes its context");

            // A call whose context is disposed fails alone; the calls running next to it on the same store are not affected.
            var broken = Task.Run(async () =>
            {
                disposeMyContext.Value = true;
                await store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", "broken") });
            });
            var healthy = Enumerable.Range(0, count).Select(i => Task.Run(() => store.SaveDeltasAsync(new List<IDelta> { store.CreateDelta("Client", "healthy " + i) }))).ToList();
            Assert.ThrowsAsync<ObjectDisposedException>(() => broken);
            await Task.WhenAll(healthy);

            AssertDistinct(await store.GetDeltasAsync(string.Empty), 2 * count);
        }

        [TestCase(Database.SqliteFile)]
        [TestCase(Database.SqliteSharedMemory)]
        public async Task ConcurrentSequenceValuesAreDistinctGapFreeAndNeverReused(Database database)
        {
            var options = new DbContextOptionsBuilder<DeltaDbContext>().UseSqlite(ConnectionString(database, "deltas")).Options;
            using (var setup = new DeltaDbContext(options))
                setup.Database.EnsureCreated();
            await AssertSequence(() => new DeltaDbContext(options));
        }

        [Test]
        public async Task ConcurrentSequenceValuesOnEfInMemoryAreDistinctGapFreeAndNeverReused()
        {
            var options = new DbContextOptionsBuilder<DeltaDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            await AssertSequence(() => new DeltaDbContext(options));
        }

        private static async Task AssertSequence(Func<DeltaDbContext> factory)
        {
            const int count = 100;
            const string prefix = "P";
            var service = new EfSequenceService(new YearSequencePrefixStrategy(), factory);
            // A second instance on the same database must not hand out the same values either.
            var other = new EfSequenceService(new YearSequencePrefixStrategy(), factory);

            using var start = new ManualResetEventSlim(false);
            var tasks = Enumerable.Range(0, count).Select(i => Task.Run(() =>
            {
                start.Wait();
                return (i % 4 == 0 ? other : service).GenerateNextSequenceAsync(prefix);
            })).ToList();
            start.Set();
            var numbers = Numbers(await Task.WhenAll(tasks), prefix);

            Assert.AreEqual(count, numbers.Distinct().Count(), "distinct values");
            var sorted = numbers.OrderBy(n => n).ToList();
            for (int i = 1; i < sorted.Count; i++)
                Assert.AreEqual(sorted[i - 1] + 1, sorted[i], "gap-free and strictly increasing");

            var later = Numbers(await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => service.GenerateNextSequenceAsync(prefix)))), prefix);
            Assert.IsTrue(later.All(n => n > sorted.Last()), "a value handed out is never returned again");
            Assert.AreEqual(10, later.Distinct().Count());
        }

        /// <summary>
        /// A UI-style context: one thread runs posted callbacks one by one, so while a callback blocks that thread,
        /// nothing else posted to it can run.
        /// </summary>
        private sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
        {
            private readonly BlockingCollection<(SendOrPostCallback, object)> queue = new BlockingCollection<(SendOrPostCallback, object)>();
            private readonly Thread thread;

            public SingleThreadSynchronizationContext()
            {
                thread = new Thread(() =>
                {
                    SetSynchronizationContext(this);
                    foreach (var (callback, state) in queue.GetConsumingEnumerable())
                        callback(state);
                }) { IsBackground = true };
                thread.Start();
            }

            public override void Post(SendOrPostCallback d, object state) => queue.Add((d, state));

            public override void Send(SendOrPostCallback d, object state) => throw new NotSupportedException();

            /// <summary>Runs <paramref name="action"/> on the context's thread; the task completes when it returns.</summary>
            public Task Run(Action action)
            {
                var done = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
                Post(_ =>
                {
                    try { action(); done.SetResult(null); }
                    catch (Exception ex) { done.SetException(ex); }
                }, null);
                return done.Task;
            }

            public void Dispose() => queue.CompleteAdding();
        }

        private static async Task<bool> CompletesWithin(Task task, TimeSpan timeout)
            => await Task.WhenAny(task, Task.Delay(timeout)) == task;

        [Test]
        public async Task TheSynchronizationContextCatchesSyncOverAsync()
        {
            // Proves the test context can catch the deadlock: blocking on an await that resumes on it never returns.
            using var ui = new SingleThreadSynchronizationContext();
            var completed = new ManualResetEventSlim(false);
            async Task Naive() { await Task.Delay(10); }
            var blocked = ui.Run(() => completed.Set(Naive().Wait(TimeSpan.FromSeconds(2))));
            await blocked;
            Assert.IsFalse(completed.IsSet, "naive sync-over-async should time out on this context");

            void Set(ManualResetEventSlim e, bool value) { if (value) e.Set(); }
        }

        [TestCase(Database.SqliteFile)]
        [TestCase(Database.SqliteSharedMemory)]
        public async Task SyncSaveChangesOnAUiSynchronizationContextReturnsAndRecordsTheDelta(Database database)
        {
            string data = ConnectionString(database, "data");
            var services = NewServices(ConnectionString(database, "deltas"));
            using var context = NewContext(data, services);
            context.Database.EnsureCreated();

            using var ui = new SingleThreadSynchronizationContext();
            var save = ui.Run(() =>
            {
                Assert.IsInstanceOf<SingleThreadSynchronizationContext>(SynchronizationContext.Current);
                context.Add(new Blog { Name = "ui thread" });
                context.SaveChanges();
            });

            Assert.IsTrue(await CompletesWithin(save, TimeSpan.FromSeconds(10)), "SaveChanges deadlocked on the UI context");
            await save;
            var store = services.GetRequiredService<IDeltaStore>();
            Assert.AreEqual(1, (await store.GetDeltasAsync(string.Empty)).Count(), "a delta is recorded");
            using var check = NewContext(data, services);
            Assert.AreEqual("ui thread", check.Blogs.Single().Name);
        }
    }
}
