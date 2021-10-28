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
    public class Tests : MultiServerBaseTest
    {
        TestClientFactory cf;
        [SetUp()]
        public override void Setup()
        {
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 26));
            base.Setup();
            cf = this.GetTestClientFactory();
            masterContextOptionBuilder.UseSqlServer("Server=MSI;Database=EfMaster;Trusted_Connection=True;");
            node_AContextOptionbuilder.UseSqlite("Data Source=EfNode_A.db;");
            node_BContextOptionbuilder.UseNpgsql("Server=127.0.0.1;User Id=postgres;Password=1234567890;Port=5432;Database=EfNode_B;");
            node_CContextOptionbuilder.UseMySql("Server=127.0.0.1;Uid=root;Pwd=1234567890;Database=EfNode_C;SslMode=Preferred;", serverVersion);
            //masterContextOptionBuilder.UseSqlite(GetSqliteMemoryConnection("Master"));
            //node_AContextOptionbuilder.UseSqlite(GetSqliteMemoryConnection("A"));
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
        public async Task SqlServerToSqliteTest()
        {

            var MasterHttpCLient = cf.CreateClient("Master");
            var Node_A_HttpClient = cf.CreateClient("Node A");
            var Node_B_HttpClient = cf.CreateClient("Node B");
            var Node_C_HttpClient = cf.CreateClient("Node C");

            ServiceCollection ServiceCollectionMaster = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_A = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_B = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_C = new ServiceCollection();

            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new SqliteDeltaGenerator());

            List<DeltaGeneratorBase> deltaGeneratorBases = new List<DeltaGeneratorBase>();
            deltaGeneratorBases.Add(new NpgsqlDeltaGenerator());
            deltaGeneratorBases.Add(new PomeloMySqlDeltaGenerator());
            deltaGeneratorBases.Add(new SqliteDeltaGenerator());
            deltaGeneratorBases.Add(new SqlServerDeltaGenerator());
            DeltaGeneratorBase[] additionalDeltaGenerators = deltaGeneratorBases.ToArray();

            ServiceCollectionMaster.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb1"); }, MasterHttpCLient, "MemoryDeltaStore1", additionalDeltaGenerators);
            ServiceCollectionMaster.AddEntityFrameworkSqlServer();
            ServiceCollectionMaster.AddSingleton<ISyncIdentityService>(new SyncIdentityService("Master"));

            ServiceCollectionNode_A.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb2"); }, Node_A_HttpClient, "MemoryDeltaStore1", additionalDeltaGenerators);
            ServiceCollectionNode_A.AddEntityFrameworkSqlite();
            ServiceCollectionNode_A.AddSingleton<ISyncIdentityService>(new SyncIdentityService("Node A"));

            ServiceCollectionNode_B.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb3"); }, Node_B_HttpClient, "MemoryDeltaStore1", additionalDeltaGenerators);
            ServiceCollectionNode_B.AddEntityFrameworkNpgsql();
            ServiceCollectionNode_B.AddSingleton<ISyncIdentityService>(new SyncIdentityService("Node B"));


            ServiceCollectionNode_B.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb4"); }, Node_C_HttpClient, "MemoryDeltaStore1", additionalDeltaGenerators);
            ServiceCollectionNode_B.AddEntityFrameworkMySql();
            ServiceCollectionNode_B.AddSingleton<ISyncIdentityService>(new SyncIdentityService("Node c"));



            using (var MasterContext = new TestSyncFrameworkDbContext(masterContextOptionBuilder.Options, ServiceCollectionMaster, "Master"))
            using (var Node_A_Context = new TestSyncFrameworkDbContext(node_AContextOptionbuilder.Options, ServiceCollectionNode_A, "Node A"))
            using (var Node_B_Context = new TestSyncFrameworkDbContext(node_BContextOptionbuilder.Options, ServiceCollectionNode_B, "Node B"))
            using (var Node_C_Context = new TestSyncFrameworkDbContext(node_CContextOptionbuilder.Options, ServiceCollectionNode_C, "Node C"))
            {

                await MasterContext.Database.EnsureDeletedAsync();
                await MasterContext.Database.EnsureCreatedAsync();

                await Node_A_Context.Database.EnsureDeletedAsync();
                await Node_A_Context.Database.EnsureCreatedAsync();


                Blog entity = GetBlog("Blog 1", "EF Core 3.1!", "EF Core 5.0!");

                MasterContext.Add(entity);
                await MasterContext.SaveChangesAsync();

                entity.Name = "Blog 1 Updated";

                await MasterContext.SaveChangesAsync();

                MasterContext.Remove(entity);


                await MasterContext.SaveChangesAsync();

                Blog TestBlog = GetBlog("Blog 2", "Post 1", "Post 2");
                MasterContext.Add(TestBlog);
                await MasterContext.SaveChangesAsync();

                await MasterContext.PushAsync();




                await Node_A_Context.PullAsync();




                var blogs = Node_A_Context.Blogs
                       .Include(blog => blog.Posts)
                       .ToList();

                var count = blogs.Count;
                Blog SqliteBlog = GetBlog("Blog 3", "SQLite post 1", "SQLite post 2");
                Node_A_Context.Add(SqliteBlog);

                await Node_A_Context.SaveChangesAsync();

                await Node_A_Context.PushAsync();


                var Deltas = await MasterContext.FetchAsync();
                await MasterContext.PullAsync();




                Blog NewBlog = GetBlog("Blog 4", "NewBlog post 1", "NewBlog post 2");
                Node_A_Context.Add(NewBlog);


                await Node_A_Context.SaveChangesAsync();
                await Node_A_Context.PushAsync();

                Deltas = await MasterContext.FetchAsync();

                await MasterContext.PullAsync();

                var SqlServerBlogs = MasterContext.Blogs
                       .Include((Blog blog) => blog.Posts)
                       .ToList();

                Assert.AreEqual(3, SqlServerBlogs.Count);
            }



        }
        private static Blog GetBlog(string Name, string Title1, string Title2)
        {
            return new Blog { Name = Name, Posts = { new Post { Title = Title1 }, new Post { Title = Title2 } } };
        }




    }
}