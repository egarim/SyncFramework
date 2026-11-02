using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Model;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Tests
{
    /// <summary>
    /// One client database whose Blogs sync with one server and whose Posts sync with another (issue #12).
    /// SQLite and in-memory servers only, so it runs without database servers.
    /// </summary>
    public class DeltaRouteTests
    {
        private const string BlogsNode = "BlogsNode";
        private const string PostsNode = "PostsNode";
        private string folder;
        private MemoryDeltaStore blogsServerStore;
        private MemoryDeltaStore postsServerStore;
        private SyncFrameworkServer blogsServer;
        private SyncFrameworkServer postsServer;

        [SetUp]
        public void SetUp()
        {
            folder = Path.Combine(Path.GetTempPath(), "SyncFrameworkDeltaRoutes_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            blogsServerStore = new MemoryDeltaStore();
            postsServerStore = new MemoryDeltaStore();
            blogsServer = new SyncFrameworkServer(new SyncServerNode(blogsServerStore, null, BlogsNode));
            postsServer = new SyncFrameworkServer(new SyncServerNode(postsServerStore, null, PostsNode));
        }

        [TearDown]
        public void TearDown()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(folder, true); } catch (IOException) { }
        }

        private TestSyncFrameworkDbContext NewClient(string identity)
        {
            string File(string name) => $"Data Source={Path.Combine(folder, identity + "-" + name + ".db")}";
            var services = new ServiceCollection();
            services.AddEfSynchronization(o => o.UseSqlite(File("deltas")), new SyncFrameworkInMemoryClient(blogsServer, BlogsNode), identity, new SqliteDeltaGenerator());
            services.AddEntityFrameworkSqlite();
            services.AddDeltaRoute("posts", o => o.UseSqlite(File("post-deltas")), new SyncFrameworkInMemoryClient(postsServer, PostsNode), "Post");
            services.AddSingleton(typeof(ISequencePrefixStrategy), new YearSequencePrefixStrategy());
            services.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            var options = new DbContextOptionsBuilder().UseSqlite(File("data")).Options;
            var context = new TestSyncFrameworkDbContext(options, services.BuildServiceProvider());
            context.Database.EnsureCreated();
            return context;
        }

        [Test]
        public async Task EachTableGoesToItsOwnServerAndComesBackFromIt()
        {
            using var writer = NewClient("Writer");
            writer.Add(new Blog { Name = "Routing", Posts = { new Post { Title = "First" }, new Post { Title = "Second" } } });
            await writer.SaveChangesAsync();

            var pushed = await writer.PushAllRoutesAsync();
            Assert.IsTrue(pushed.All(r => r.Success), string.Join("; ", pushed.Select(r => r.Message)));

            var blogDeltas = (await blogsServerStore.GetDeltasAsync("")).ToList();
            var postDeltas = (await postsServerStore.GetDeltasAsync("")).ToList();
            Assert.AreEqual(1, blogDeltas.Count, "the blog went to the blogs server");
            Assert.AreEqual(1, postDeltas.Count, "the posts went to the posts server");
            string Sql(IDelta delta) => System.Text.Encoding.UTF8.GetString(blogsServerStore.Decompress(delta.Operation));
            StringAssert.Contains("INTO \\\"Post\\\"", Sql(postDeltas[0]), "the posts server got post SQL");
            StringAssert.DoesNotContain("INTO \\\"Post\\\"", Sql(blogDeltas[0]), "no post SQL on the blogs server");
            StringAssert.Contains("INTO \\\"Blogs\\\"", Sql(blogDeltas[0]), "the blogs server got blog SQL");

            using var reader = NewClient("Reader");
            var pulled = await reader.PullAllRoutesAsync();
            Assert.IsTrue(pulled.All(r => r.Success), string.Join("; ", pulled.Select(r => r.Message)));
            Assert.AreEqual(1, reader.Blogs.Count());
            Assert.AreEqual(2, reader.Set<Post>().Count());

            // Positions are kept per route: pulling again gets nothing from either server.
            var again = await reader.PullAllRoutesAsync();
            Assert.IsTrue(again.All(r => r.Message == "Nothing to receive"), string.Join("; ", again.Select(r => r.Message)));
        }

        [Test]
        public async Task ARouteCanBePushedAndPulledOnItsOwn()
        {
            using var writer = NewClient("Writer");
            writer.Add(new Blog { Name = "Only posts move", Posts = { new Post { Title = "Hello" } } });
            await writer.SaveChangesAsync();

            Assert.IsTrue((await writer.PushAsync("posts")).Success);
            Assert.AreEqual(0, (await blogsServerStore.GetDeltasAsync("")).Count(), "the default route was not pushed");
            Assert.AreEqual(1, (await postsServerStore.GetDeltasAsync("")).Count());

            Assert.Throws<ArgumentException>(() => writer.ForRoute("nope"));
        }

        [Test]
        public async Task WithoutRoutesEverythingStaysOnTheDefaultStore()
        {
            var services = new ServiceCollection();
            services.AddSyncFrameworkForSQLite($"Data Source={Path.Combine(folder, "plain-deltas.db")}",
                new System.Net.Http.HttpClient { BaseAddress = new Uri("http://localhost/sync/") }, "Node", "Plain", new SqliteDeltaGenerator());
            services.AddSingleton(typeof(ISequencePrefixStrategy), new YearSequencePrefixStrategy());
            services.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            using var context = new TestSyncFrameworkDbContext(
                new DbContextOptionsBuilder().UseSqlite($"Data Source={Path.Combine(folder, "plain.db")}").Options, services.BuildServiceProvider());
            context.Database.EnsureCreated();

            context.Add(new Blog { Name = "Plain", Posts = { new Post { Title = "Post" } } });
            await context.SaveChangesAsync();

            Assert.AreEqual(0, context.Routes.Count);
            Assert.AreEqual(1, await context.DeltaStore.GetDeltaCountAsync("", "Plain", default));
        }
    }
}
