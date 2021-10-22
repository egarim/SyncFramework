using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore.Sqlite;
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
            base.Setup();
            cf = this.GetTestClientFactory();
        }
        private const string InMemoryConnectionString = "DataSource=:memory:";

       
        SqliteConnection GetSqliteMemoryConnection()
        {
            var connection = new SqliteConnection(InMemoryConnectionString);
            connection.Open();
            return connection;
        }
        [Test]
        public async Task SqlServerToSqliteTest()
        {

            var MasterHttpCLient = cf.CreateClient("TestClient");
            var Client_AHttpClient = cf.CreateClient("TestClient");



            ServiceCollection Master = new ServiceCollection();
            ServiceCollection Client_A = new ServiceCollection();

            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new SqliteDeltaGenerator());
            

            Master.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb1"); }, MasterHttpCLient, "MemoryDeltaStore1");
            Master.AddEntityFrameworkSqlite();
            Master.AddSingleton<ISyncIdentityService>(new SyncIdentityService("Master"));

            Client_A.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb2"); }, Client_AHttpClient, "MemoryDeltaStore1");
            Client_A.AddEntityFrameworkSqlite();
            Client_A.AddSingleton<ISyncIdentityService>(new SyncIdentityService("A"));



            DbContextOptionsBuilder SqlServerOptionsBuilder = new DbContextOptionsBuilder();
            SqlServerOptionsBuilder.UseSqlite("Data Source=Test.db;");

            DbContextOptionsBuilder SqliteOptionsBuilder = new DbContextOptionsBuilder();
            SqliteOptionsBuilder.UseSqlite("Data Source=Test.db;");


            using (var SqliteContext = new TestSyncFrameworkDbContext(SqliteOptionsBuilder.Options, Client_A, "Client A"))
            using (var SqlServerContext = new TestSyncFrameworkDbContext(SqlServerOptionsBuilder.Options, Master, "Master"))
            {

                await SqlServerContext.Database.EnsureDeletedAsync();
                await SqlServerContext.Database.EnsureCreatedAsync();

                await SqliteContext.Database.EnsureDeletedAsync();
                await SqliteContext.Database.EnsureCreatedAsync();


                Blog entity = GetBlog("Blog 1", "EF Core 3.1!", "EF Core 5.0!");

                SqlServerContext.Add(entity);
                await SqlServerContext.SaveChangesAsync();

                entity.Name = "Blog 1 Updated";

                await SqlServerContext.SaveChangesAsync();

                SqlServerContext.Remove(entity);


                await SqlServerContext.SaveChangesAsync();

                Blog TestBlog = GetBlog("Blog 2", "Post 1", "Post 2");
                SqlServerContext.Add(TestBlog);
                await SqlServerContext.SaveChangesAsync();

                await SqlServerContext.PushAsync();




                await SqliteContext.PullAsync();




                var blogs = SqliteContext.Blogs
                       .Include(blog => blog.Posts)
                       .ToList();

                var count = blogs.Count;
                Blog SqliteBlog = GetBlog("Blog 3", "SQLite post 1", "SQLite post 2");
                SqliteContext.Add(SqliteBlog);

                await SqliteContext.SaveChangesAsync();

                await SqliteContext.PushAsync();


                var Deltas = await SqlServerContext.FetchAsync();
                await SqlServerContext.PullAsync();




                Blog NewBlog = GetBlog("Blog 4", "NewBlog post 1", "NewBlog post 2");
                SqliteContext.Add(NewBlog);


                await SqliteContext.SaveChangesAsync();
                await SqliteContext.PushAsync();

                Deltas = await SqlServerContext.FetchAsync();

                await SqlServerContext.PullAsync();

                var SqlServerBlogs = SqlServerContext.Blogs
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