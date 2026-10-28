using BIT.Data.Sync.Client;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Tests.InMemoryServerTests
{
    /// <summary>
    /// The whole round trip with no HTTP and no test host: an in-process <see cref="SyncFrameworkServer"/> over a
    /// <see cref="MemoryDeltaStore"/>, and clients that reach it through <see cref="SyncFrameworkInMemoryClient"/>.
    /// </summary>
    public class InMemorySyncServerTests
    {
        private const string NodeId = "InMemoryNode";

        private static (SyncFrameworkServer Server, Func<string, SimpleDatabase> NewDatabase) Setup()
        {
            var server = new SyncFrameworkServer(new SyncServerNode(new MemoryDeltaStore(), null, NodeId));
            return (server, identity => new SimpleDatabase(identity, new SyncFrameworkInMemoryClient(server, NodeId)));
        }

        [Test]
        public async Task ThreeDatabasesConvergeThroughAnInMemoryServer()
        {
            var (_, newDatabase) = Setup();
            var master = newDatabase("Master");
            var a = newDatabase("A");
            var b = newDatabase("B");

            var hello = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Hello" };
            var hola = new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Hola" };
            await master.Add(hello);
            await master.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "World" });
            await a.Add(hola);
            await a.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Mundo" });
            await b.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Privet" });
            await b.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "Mir" });

            foreach (var db in new[] { master, a, b })
            {
                var pushed = await db.PushAsync();
                Assert.IsTrue(pushed.Success, pushed.Message);
            }
            foreach (var db in new[] { master, a, b })
            {
                var pulled = await db.PullAsync();
                Assert.IsTrue(pulled.Success, pulled.Message);
            }

            Assert.AreEqual(6, master.Data.Count, "master");
            Assert.AreEqual(6, a.Data.Count, "A");
            Assert.AreEqual(6, b.Data.Count, "B");

            // An update and a delete made on one database reach the others on their next pull.
            hello.Text = "HELLO";
            await master.Update(hello);
            await master.Delete(hola);
            Assert.IsTrue((await master.PushAsync()).Success);
            foreach (var db in new[] { a, b })
            {
                var pulled = await db.PullAsync();
                Assert.IsTrue(pulled.Success, pulled.Message);
            }

            foreach (var db in new[] { a, b })
            {
                Assert.AreEqual(5, db.Data.Count, db.Identity);
                Assert.AreEqual("HELLO", db.Data.Single(r => r.Key == hello.Key).Text, db.Identity);
                Assert.IsFalse(db.Data.Any(r => r.Key == hola.Key), db.Identity);
            }

            var again = await a.PullAsync();
            Assert.IsTrue(again.Success);
            Assert.AreEqual("Nothing to receive", again.Message);
        }

        [Test]
        public async Task PushingDoesNotRenumberTheClientsOwnDeltas()
        {
            // The server numbers what it saves in its own sequence. A first client's deltas get the same numbers they
            // already had, so the check needs a second client: the server gives its deltas higher numbers, and without
            // a copy at the client boundary those numbers would be written into the second client's own store.
            var (_, newDatabase) = Setup();
            var first = newDatabase("First");
            var second = newDatabase("Second");
            await first.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "one" });
            await first.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "two" });
            await second.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "three" });
            await second.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "four" });
            var before = (await second.DeltaStore.GetDeltasAsync("", default)).Select(d => d.Index).ToList();

            Assert.IsTrue((await first.PushAsync()).Success);
            Assert.IsTrue((await second.PushAsync()).Success);

            var after = (await second.DeltaStore.GetDeltasAsync("", default)).Select(d => d.Index).ToList();
            CollectionAssert.AreEqual(before, after);
        }

        [Test]
        public async Task AnUnknownNodeFailsWithAMessageInsteadOfDroppingTheDeltas()
        {
            var (server, _) = Setup();
            var client = new SimpleDatabase("Lost", new SyncFrameworkInMemoryClient(server, "NoSuchNode"));
            await client.Add(new SimpleDatabaseRecord { Key = Guid.NewGuid(), Text = "orphan" });

            var pushed = await client.PushAsync();
            Assert.IsFalse(pushed.Success);
            StringAssert.Contains("NoSuchNode", pushed.Message);

            var fetched = await client.FetchAsync();
            Assert.IsFalse(fetched.Success);
        }

        [Test]
        public async Task HandShakeAnswersFromTheServer()
        {
            var (server, _) = Setup();
            var response = await new SyncFrameworkInMemoryClient(server, NodeId).HandShake(default);
            Assert.IsTrue(response.Success);
            Assert.AreEqual(NodeId, response.ServerNodeId);
        }
    }
}
