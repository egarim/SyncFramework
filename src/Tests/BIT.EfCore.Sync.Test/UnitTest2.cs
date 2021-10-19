using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.EfCore.Sync.Test.Contexts.SyncFramework;
using BIT.EfCore.Sync.Test.Infrastructure;
using BIT.EfCore.Sync.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SyncFrameworkTests.EF.Sqlite;
using SyncFrameworkTests.EF.SqlServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BIT.EfCore.Sync.Test
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
        [Test]
        public async Task SqlServerToSqliteTest()
        {
            try
            {

            }
            catch (Exception ex)
            {
                var message = ex.Message;
                throw;
            }

            var client = cf.CreateClient("TestClient");
            client.DefaultRequestHeaders.Add("DeltaStoreName", "MemoryDeltaStore1");
            client.DefaultRequestHeaders.Add("DeltaProcessorName", "MemoryDeltaStore2");


            ServiceCollection SqlServerServiceCollection = new ServiceCollection();
            ServiceCollection SqlLiteServiceCollection = new ServiceCollection();

            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new SqlServerDeltaGenerator());
            DeltaGenerators.Add(new SqliteDeltaGenerator());

            SqlServerServiceCollection.AddEfSynchronization((options) => { options.UseInMemoryDatabase("SqlServerInMemory"); }, client, DeltaGenerators);
            SqlServerServiceCollection.AddEntityFrameworkSqlServer();

            SqlLiteServiceCollection.AddEfSynchronization((options) => { options.UseInMemoryDatabase("SqlLiteInMemory"); }, client, DeltaGenerators);
            SqlLiteServiceCollection.AddEntityFrameworkSqlite();



            DbContextOptionsBuilder SqlServerOptionsBuilder = new DbContextOptionsBuilder();
            SqlServerOptionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=EFBlogNew;Trusted_Connection=True;");

            DbContextOptionsBuilder SqliteOptionsBuilder = new DbContextOptionsBuilder();
            SqliteOptionsBuilder.UseSqlite("Data Source=Test.db;");


            using (var SqliteContext = new SqliteSyncDbContext(SqliteOptionsBuilder.Options, SqlLiteServiceCollection, "Client A"))
            using (var SqlServerContext = new SqlServerSyncDbContext(SqlServerOptionsBuilder.Options, SqlServerServiceCollection, "Master"))
            {

                await SqlServerContext.Database.EnsureDeletedAsync();
                await SqlServerContext.Database.EnsureCreatedAsync();

                await SqliteContext.Database.EnsureDeletedAsync();
                await SqliteContext.Database.EnsureCreatedAsync();


                Blog entity = GetBlog("EF Blog", "EF Core 3.1!", "EF Core 5.0!");

                SqlServerContext.Add(
                    entity);
                await SqlServerContext.SaveChangesAsync();

                entity.Name = "Updated Name";

                await SqlServerContext.SaveChangesAsync();

                SqlServerContext.Remove(entity);


                await SqlServerContext.SaveChangesAsync();

                Blog TestBlog = GetBlog("Test blog", "Post 1", "Post 2");
                SqlServerContext.Add(TestBlog);
                await SqlServerContext.SaveChangesAsync();

                await SqlServerContext.PushAsync();




                await SqliteContext.PullAsync();




                var blogs = SqliteContext.Blogs
                       .Include(blog => blog.Posts)
                       .ToList();

                var count = blogs.Count;
                Blog SqliteBlog = GetBlog("SqliteBlog", "SQLite post 1", "SQLite post 2");
                SqliteContext.Add(SqliteBlog);

                await SqliteContext.SaveChangesAsync();

                await SqliteContext.PushAsync();


                var Deltas = await SqlServerContext.FetchAsync();
                await SqlServerContext.PullAsync();




                Blog NewBlog = GetBlog("NewBlog", "NewBlog post 1", "NewBlog post 2");
                SqliteContext.Add(NewBlog);


                await SqliteContext.SaveChangesAsync();
                await SqliteContext.PushAsync();

                Deltas = await SqlServerContext.FetchAsync();

                await SqlServerContext.PullAsync();

                var SqlServerBlogs = SqlServerContext.Blogs
                       .Include(blog => blog.Posts)
                       .ToList();

                Assert.AreEqual(3, SqlServerBlogs.Count);
            }


        }
        private static Blog GetBlog(string Name, string Title1, string Title2)
        {
            return new Blog { Name = Name, Posts = { new Post { Title = Title1 }, new Post { Title = Title2 } } };
        }



        [Test]
        public void Test2()
        {
            for (int i = 0; i < 100; i++)
            {
                Debug.WriteLine(Delta.GenerateComb().ToString());
            }
            Assert.Pass();
        }
    }
}