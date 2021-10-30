using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.Sqlite;
using BIT.Data.Sync.EfCore.SqlServer;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Infrastructure;
using BIT.Data.Sync.EfCore.Tests.Model;
using BIT.EfCore.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore.Tests
{
    public class AllDatabaseTests : MultiServerBaseTest
    {
        TestClientFactory HttpClientFactory;
        [SetUp()]
        public override void Setup()
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 26));
            base.Setup();
            HttpClientFactory = this.GetTestClientFactory();
            masterContextOptionBuilder.UseSqlServer("Server=MSI;Database=EfMaster;Trusted_Connection=True;");
            node_AContextOptionbuilder.UseSqlite("Data Source=EfNode_A.db;");
            node_BContextOptionbuilder.UseNpgsql("Server=127.0.0.1;User Id=postgres;Password=1234567890;Port=5432;Database=EfNode_B;");
            node_CContextOptionbuilder.UseMySql("Server=127.0.0.1;Uid=root;Pwd=1234567890;Database=EfNode_C;SslMode=Preferred;", serverVersion);
            
        }
        private const string InMemoryConnectionString = "DataSource=:memory:";
        DbContextOptionsBuilder masterContextOptionBuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_AContextOptionbuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_BContextOptionbuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_CContextOptionbuilder = new DbContextOptionsBuilder();
        SqliteConnection GetSqliteMemoryConnection(string Name)
        {
            var connection = new SqliteConnection($"Data Source={Name};Mode=Memory;");
            connection.Open();
            return connection;
        }
        [Test]
        public async Task MainTest()
        {

            var MasterHttpCLient = HttpClientFactory.CreateClient("Master");
            var Node_A_HttpClient = HttpClientFactory.CreateClient("Node A");
            var Node_B_HttpClient = HttpClientFactory.CreateClient("Node B");
            var Node_C_HttpClient = HttpClientFactory.CreateClient("Node C");

            ServiceCollection ServiceCollectionMaster = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_A = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_B = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_C = new ServiceCollection();

           

            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new NpgsqlDeltaGenerator());
            DeltaGenerators.Add(new PomeloMySqlDeltaGenerator());
            DeltaGenerators.Add(new SqliteDeltaGenerator());
            DeltaGenerators.Add(new SqlServerDeltaGenerator());
            DeltaGeneratorBase[] additionalDeltaGenerators = DeltaGenerators.ToArray();

            ServiceCollectionMaster.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb1"); }, MasterHttpCLient, "MemoryDeltaStore1", "Master", additionalDeltaGenerators);
            ServiceCollectionMaster.AddEntityFrameworkSqlServer();


            ServiceCollectionNode_A.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb2"); }, Node_A_HttpClient, "MemoryDeltaStore1", "Node A", additionalDeltaGenerators);
            ServiceCollectionNode_A.AddEntityFrameworkSqlite();
         

            ServiceCollectionNode_B.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb3"); }, Node_B_HttpClient, "MemoryDeltaStore1", "Node B", additionalDeltaGenerators);
            ServiceCollectionNode_B.AddEntityFrameworkNpgsql();
          


            ServiceCollectionNode_C.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb4"); }, Node_C_HttpClient, "MemoryDeltaStore1", "Node C", additionalDeltaGenerators);
            ServiceCollectionNode_C.AddEntityFrameworkMySql();
            



            using (var MasterContext = new TestSyncFrameworkDbContext(masterContextOptionBuilder.Options, ServiceCollectionMaster))
            using (var Node_A_Context = new TestSyncFrameworkDbContext(node_AContextOptionbuilder.Options, ServiceCollectionNode_A))
            using (var Node_B_Context = new TestSyncFrameworkDbContext(node_BContextOptionbuilder.Options, ServiceCollectionNode_B))
            using (var Node_C_Context = new TestSyncFrameworkDbContext(node_CContextOptionbuilder.Options, ServiceCollectionNode_C))
            {

                await MasterContext.Database.EnsureDeletedAsync();
                await MasterContext.Database.EnsureCreatedAsync();

                await Node_A_Context.Database.EnsureDeletedAsync();
                await Node_A_Context.Database.EnsureCreatedAsync();

                await Node_B_Context.Database.EnsureDeletedAsync();
                await Node_B_Context.Database.EnsureCreatedAsync();

                await Node_C_Context.Database.EnsureDeletedAsync();
                await Node_C_Context.Database.EnsureCreatedAsync();

                Blog SqlServerBlog = GetBlog("SqlServer blog", "EF Core 3.1!", "EF Core 5.0!");
                Blog SqliteBlog = GetBlog("Sqlite blog", "EF Core 3.1!", "EF Core 5.0!");
                Blog NpgsqlBlog = GetBlog("Npgsql blog", "EF Core 3.1!", "EF Core 5.0!");
                Blog PomeloMySqlBlog = GetBlog("Pomelo MySql blog", "EF Core 3.1!", "EF Core 5.0!");

                MasterContext.Add(SqlServerBlog);
                MasterContext.Add(SqliteBlog);
                MasterContext.Add(NpgsqlBlog);
                MasterContext.Add(PomeloMySqlBlog);
                await MasterContext.SaveChangesAsync();
                await MasterContext.PushAsync();

             
                await Node_A_Context.PullAsync();
                await Node_B_Context.PullAsync();
                await Node_C_Context.PullAsync();

                Node_A_Context.Add(GetBlog("Node A Blog","Post 1","Post 2"));
                Node_B_Context.Add(GetBlog("Node B Blog", "Post 1", "Post 2"));
                Node_C_Context.Add(GetBlog("Node C Blog", "Post 1", "Post 2"));


                await Node_A_Context.SaveChangesAsync();
                await Node_A_Context.PushAsync();

                await Node_B_Context.SaveChangesAsync();
                await Node_B_Context.PushAsync();

                await Node_C_Context.SaveChangesAsync();
                await Node_C_Context.PushAsync();


                await MasterContext.PullAsync();

                var blogs = MasterContext.Blogs.ToList();
                var count = blogs.Count;

                Assert.AreEqual(7, count);


                await Node_A_Context.PullAsync();
                await Node_B_Context.PullAsync();
                await Node_C_Context.PullAsync();


                Assert.AreEqual(7, Node_A_Context.Blogs.Count());
                Assert.AreEqual(7, Node_A_Context.Blogs.Count());
                Assert.AreEqual(7, Node_A_Context.Blogs.Count());
            }



        }
        private static Blog GetBlog(string Name, string Title1, string Title2)
        {
            return new Blog { Name = Name, Posts = { new Post { Title = Title1 }, new Post { Title = Title2 } } };
        }




    }
}