using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.Pomelo.MySql;
using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.SqlServer;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Infrastructure;
using BIT.Data.Sync.EfCore.Tests.Model;
using BIT.Data.Sync.Imp;
using BIT.EfCore.Sync;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Npgsql;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Tests
{
    public class AllDatabaseTests : MultiServerBaseTest
    {
        #region Connection Strings
        private string SqlServerSyncFrameworkTestCnx;
        private string SqlServerSyncFrameworkTestDeltaCnx;
        private string PostgresSyncFrameworkTestCnx;
        private string PostgresSyncFrameworkTestDeltaCnx;
        private string SQLiteSyncFrameworkTestCnx;
        private string SQLiteSyncFrameworkTestDeltaCnx;
        private string MySQLSyncFrameworkTestCnx;
        private string MySQLSyncFrameworkTestDeltaCnx;
        #endregion

        #region Context Configuration
        private DbContextOptionsBuilder masterContextOptionBuilder = new DbContextOptionsBuilder();
        private DbContextOptionsBuilder node_AContextOptionBuilder = new DbContextOptionsBuilder();
        private DbContextOptionsBuilder node_BContextOptionBuilder = new DbContextOptionsBuilder();
        private DbContextOptionsBuilder node_CContextOptionBuilder = new DbContextOptionsBuilder();
        private MySqlServerVersion serverVersion;
        private TestClientFactory HttpClientFactory;
        #endregion

        #region Setup and Cleanup
        [SetUp]
        public override void Setup()
        {
            // Load connection strings from environment variables
            LoadConnectionStrings();

            // Set MySQL server version
            serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

            base.Setup();
            HttpClientFactory = this.GetTestClientFactory();

            // Configure context options
            masterContextOptionBuilder.UseSqlServer(SqlServerSyncFrameworkTestCnx);
            node_AContextOptionBuilder.UseSqlite(SQLiteSyncFrameworkTestCnx);
            node_BContextOptionBuilder.UseNpgsql(PostgresSyncFrameworkTestCnx);
            node_CContextOptionBuilder.UseMySql(MySQLSyncFrameworkTestCnx, serverVersion);
        }

        private void LoadConnectionStrings()
        {
            SqlServerSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(SqlServerSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
            SqlServerSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(SqlServerSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);
            PostgresSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(PostgresSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
            PostgresSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(PostgresSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);
            SQLiteSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(SQLiteSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
            SQLiteSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(SQLiteSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);
            MySQLSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(MySQLSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
            MySQLSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(MySQLSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);
        }

        private void CleanupDatabases()
        {
            DropPostgres(PostgresSyncFrameworkTestCnx);
            DropPostgres(PostgresSyncFrameworkTestDeltaCnx);
            DropMysql(MySQLSyncFrameworkTestCnx);
            DropMysql(MySQLSyncFrameworkTestDeltaCnx);
            DropSqlServer(SqlServerSyncFrameworkTestCnx);
            DropSqlServer(SqlServerSyncFrameworkTestDeltaCnx);
            DropSQLite(SQLiteSyncFrameworkTestCnx);
            DropSQLite(SQLiteSyncFrameworkTestDeltaCnx);
        }
        #endregion

        #region Database Cleanup Helpers
        private void DropMysql(string cnx)
        {
            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(cnx);
                string databaseName = connectionStringParserService.GetPartByName("Database");
                connectionStringParserService.RemovePartByName("Database");

                using (var connection = new MySqlConnection(cnx))
                {
                    connection.Open();
                    using (var cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = $"DROP DATABASE IF EXISTS {databaseName}";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { /* Ignore errors during cleanup */ }
        }

        private void DropPostgres(string cnx)
        {
            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(cnx);
                string databaseName = connectionStringParserService.GetPartByName("Database");
                connectionStringParserService.RemovePartByName("Database");

                cnx = connectionStringParserService.GetConnectionString();
                using (var connection = new NpgsqlConnection(cnx))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { /* Ignore errors during cleanup */ }
        }

        private void DropSqlServer(string cnx)
        {
            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(cnx);
                string databaseName = connectionStringParserService.GetPartByName("Database");
                connectionStringParserService.RemovePartByName("Database");

                // Connect to the "master" database to drop the database
                connectionStringParserService.UpdatePartByName("Database", "master");
                cnx = connectionStringParserService.GetConnectionString();

                using (var connection = new SqlConnection(cnx))
                {
                    connection.Open();
                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;
                        cmd.CommandText = $"ALTER DATABASE {databaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {databaseName};";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { /* Ignore errors during cleanup */ }
        }

        private void DropSQLite(string cnx)
        {
            ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(cnx);
            string databaseName = connectionStringParserService.GetPartByName("Data Source");
            connectionStringParserService.RemovePartByName("Data Source");
            if (File.Exists(databaseName))
            {
                File.Delete(databaseName);
            }
        }
        #endregion

        #region Service Configuration
        private ServiceProvider ConfigureServices(string nodeType, HttpClient client, string connectionString, MySqlServerVersion mysqlVersion = null)
        {
            ServiceCollection services = new ServiceCollection();
            DeltaGeneratorBase[] deltaGenerators = GetDeltaGenerators();

            switch (nodeType)
            {
                case "Master":
                    services.AddSyncFrameworkForSqlServer(connectionString, client, "MemoryDeltaStore1", nodeType, deltaGenerators);
                    break;
                case "Node A":
                    services.AddSyncFrameworkForSQLite(connectionString, client, "MemoryDeltaStore1", nodeType, deltaGenerators);
                    break;
                case "Node B":
                    services.AddSyncFrameworkForNpgsql(connectionString, client, "MemoryDeltaStore1", nodeType, deltaGenerators);
                    break;
                case "Node C":
                    services.AddSyncFrameworkForMysql(connectionString, mysqlVersion, client, "MemoryDeltaStore1", nodeType, deltaGenerators);
                    break;
                default:
                    throw new ArgumentException("Unknown node type", nameof(nodeType));
            }

            // Add sequence services
            services.AddSingleton(typeof(ISequencePrefixStrategy), new YearSequencePrefixStrategy());
            services.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            return services.BuildServiceProvider();
        }

        private DeltaGeneratorBase[] GetDeltaGenerators()
        {
            List<DeltaGeneratorBase> deltaGenerators = new List<DeltaGeneratorBase>
            {
                new NpgsqlDeltaGenerator(),
                new PomeloMySqlDeltaGenerator(serverVersion),
                new SqliteDeltaGenerator(),
                new SqlServerDeltaGenerator()
            };
            return deltaGenerators.ToArray();
        }
        #endregion

        #region Helper Methods
        private static Blog GetBlog(string name, string title1, string title2)
        {
            return new Blog { Name = name, Posts = { new Post { Title = title1 }, new Post { Title = title2 } } };
        }

        private async Task<(TestSyncFrameworkDbContext master, TestSyncFrameworkDbContext nodeA, TestSyncFrameworkDbContext nodeB, TestSyncFrameworkDbContext nodeC)>
            SetupContexts()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            EfDeltaStore.EnsureDeleted = true;
            CleanupDatabases();

            // Configure HTTP clients
            var masterHttpClient = ConfigureHttpClient(HttpClientFactory.CreateClient("Master"));
            var nodeAHttpClient = ConfigureHttpClient(HttpClientFactory.CreateClient("Node A"));
            var nodeBHttpClient = ConfigureHttpClient(HttpClientFactory.CreateClient("Node B"));
            var nodeCHttpClient = ConfigureHttpClient(HttpClientFactory.CreateClient("Node C"));

            // Configure services
            var masterServiceProvider = ConfigureServices("Master", masterHttpClient, SqlServerSyncFrameworkTestDeltaCnx);
            var nodeAServiceProvider = ConfigureServices("Node A", nodeAHttpClient, SQLiteSyncFrameworkTestDeltaCnx);
            var nodeBServiceProvider = ConfigureServices("Node B", nodeBHttpClient, PostgresSyncFrameworkTestDeltaCnx);
            var nodeCServiceProvider = ConfigureServices("Node C", nodeCHttpClient, MySQLSyncFrameworkTestDeltaCnx, serverVersion);

            // Create contexts
            var masterContext = new TestSyncFrameworkDbContext(masterContextOptionBuilder.Options, masterServiceProvider);
            var nodeAContext = new TestSyncFrameworkDbContext(node_AContextOptionBuilder.Options, nodeAServiceProvider);
            var nodeBContext = new TestSyncFrameworkDbContext(node_BContextOptionBuilder.Options, nodeBServiceProvider);
            var nodeCContext = new TestSyncFrameworkDbContext(node_CContextOptionBuilder.Options, nodeCServiceProvider);

            // Create databases
            await masterContext.Database.EnsureDeletedAsync();
            await masterContext.Database.EnsureCreatedAsync();
            await nodeAContext.Database.EnsureDeletedAsync();
            await nodeAContext.Database.EnsureCreatedAsync();
            await nodeBContext.Database.EnsureDeletedAsync();
            await nodeBContext.Database.EnsureCreatedAsync();
            await nodeCContext.Database.EnsureDeletedAsync();
            await nodeCContext.Database.EnsureCreatedAsync();

            return (masterContext, nodeAContext, nodeBContext, nodeCContext);
        }

        private HttpClient ConfigureHttpClient(HttpClient client)
        {
            client.BaseAddress = new Uri("http://localhost/sync/");
            return client;
        }
        #endregion

        #region Tests
        [Test]
        public async Task SyncFramework_CrossDatabaseSync_SuccessfullyExchangesData()
        {
            var (masterContext, nodeAContext, nodeBContext, nodeCContext) = await SetupContexts();

            using (masterContext)
            using (nodeAContext)
            using (nodeBContext)
            using (nodeCContext)
            {
                // Step 1: Add initial data to master and push to all nodes
                await AddInitialDataAndPushFromMaster(masterContext, nodeAContext, nodeBContext, nodeCContext);

                // Step 2: Add data from each node and push
                await AddDataFromNodesAndPush(nodeAContext, nodeBContext, nodeCContext);

                // Step 3: Pull to master and verify
                await PullToMasterAndVerify(masterContext);

                // Step 4: Pull to all nodes and verify
                await PullToNodesAndVerify(nodeAContext, nodeBContext, nodeCContext);

                // Step 5: Add more data from each node and verify final sync
                await AddMoreDataAndVerifyFinalSync(masterContext, nodeAContext, nodeBContext, nodeCContext);
            }
        }

        private async Task AddInitialDataAndPushFromMaster(
            TestSyncFrameworkDbContext masterContext,
            TestSyncFrameworkDbContext nodeAContext,
            TestSyncFrameworkDbContext nodeBContext,
            TestSyncFrameworkDbContext nodeCContext)
        {
            // Add blogs to master database
            masterContext.Add(GetBlog("SqlServer blog", "EF Core 3.1!", "EF Core 5.0!"));
            masterContext.Add(GetBlog("Sqlite blog", "EF Core 3.1!", "EF Core 5.0!"));
            masterContext.Add(GetBlog("Npgsql blog", "EF Core 3.1!", "EF Core 5.0!"));
            masterContext.Add(GetBlog("Pomelo MySql blog", "EF Core 3.1!", "EF Core 5.0!"));
            await masterContext.SaveChangesAsync();

            // Push changes from master
            await masterContext.PushAsync();

            // Pull changes to all nodes
            await nodeAContext.PullAsync();
            await nodeBContext.PullAsync();
            await nodeCContext.PullAsync();

            // Verify each node has 4 blogs
            Assert.AreEqual(4, nodeAContext.Blogs.Count());
            Assert.AreEqual(4, nodeBContext.Blogs.Count());
            Assert.AreEqual(4, nodeCContext.Blogs.Count());
        }

        private async Task AddDataFromNodesAndPush(
            TestSyncFrameworkDbContext nodeAContext,
            TestSyncFrameworkDbContext nodeBContext,
            TestSyncFrameworkDbContext nodeCContext)
        {
            // Add one blog to each node
            nodeAContext.Add(GetBlog("Node A Blog", "Post 1", "Post 2"));
            nodeBContext.Add(GetBlog("Node B Blog", "Post 1", "Post 2"));
            nodeCContext.Add(GetBlog("Node C Blog", "Post 1", "Post 2"));

            // Save and push changes from each node
            await nodeAContext.SaveChangesAsync();
            await nodeAContext.PushAsync();

            await nodeBContext.SaveChangesAsync();
            await nodeBContext.PushAsync();

            await nodeCContext.SaveChangesAsync();
            await nodeCContext.PushAsync();

            // Verify each node still has 5 blogs (their original 4 + their new 1)
            Assert.AreEqual(5, nodeAContext.Blogs.Count());
            Assert.AreEqual(5, nodeBContext.Blogs.Count());
            Assert.AreEqual(5, nodeCContext.Blogs.Count());
        }

        private async Task PullToMasterAndVerify(TestSyncFrameworkDbContext masterContext)
        {
            // Pull all changes to master
            await masterContext.PullAsync();

            // Verify master now has 7 blogs (original 4 + 1 from each node)
            var blogs = masterContext.Blogs.ToList();
            Assert.AreEqual(7, blogs.Count);
        }

        private async Task PullToNodesAndVerify(
            TestSyncFrameworkDbContext nodeAContext,
            TestSyncFrameworkDbContext nodeBContext,
            TestSyncFrameworkDbContext nodeCContext)
        {
            // Fetch and pull changes to all nodes
            await nodeAContext.FetchAsync();
            await nodeBContext.FetchAsync();
            await nodeCContext.FetchAsync();

            await nodeAContext.PullAsync();
            await nodeBContext.PullAsync();
            await nodeCContext.PullAsync();

            // Verify each node now has 7 blogs
            Assert.AreEqual(7, nodeAContext.Blogs.Count());
            Assert.AreEqual(7, nodeBContext.Blogs.Count());
            Assert.AreEqual(7, nodeCContext.Blogs.Count());
        }

        private async Task AddMoreDataAndVerifyFinalSync(
            TestSyncFrameworkDbContext masterContext,
            TestSyncFrameworkDbContext nodeAContext,
            TestSyncFrameworkDbContext nodeBContext,
            TestSyncFrameworkDbContext nodeCContext)
        {
            // Add another blog to each node
            nodeAContext.Add(GetBlog("Node A Blog", "Post 2", "Post 3"));
            nodeBContext.Add(GetBlog("Node B Blog", "Post 2", "Post 3"));
            nodeCContext.Add(GetBlog("Node C Blog", "Post 2", "Post 3"));

            // Save and push changes from each node
            await nodeAContext.SaveChangesAsync();
            await nodeAContext.PushAsync();

            await nodeBContext.SaveChangesAsync();
            await nodeBContext.PushAsync();

            await nodeCContext.SaveChangesAsync();
            await nodeCContext.PushAsync();

            // Verify each node now has 8 blogs
            Assert.AreEqual(8, nodeAContext.Blogs.Count());
            Assert.AreEqual(8, nodeBContext.Blogs.Count());
            Assert.AreEqual(8, nodeCContext.Blogs.Count());

            // Pull to master and verify 10 blogs total
            await masterContext.PullAsync();
            Assert.AreEqual(10, masterContext.Blogs.Count());
        }
        #endregion
    }
}