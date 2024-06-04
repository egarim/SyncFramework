using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.SqlServer;
using BIT.Data.Sync.EfCore.Tests.Contexts.SyncFramework;
using BIT.Data.Sync.EfCore.Tests.Infrastructure;
using BIT.Data.Sync.EfCore.Tests.Model;
using BIT.Data.Sync.Imp;
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
using BIT.Data.Sync.EfCore.Pomelo.MySql;
using Npgsql;
using MySqlConnector;
using Microsoft.Data.SqlClient;
using System.IO;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BIT.Data.Sync.EfCore.Tests
{
    public class AllDatabaseTests : MultiServerBaseTest
    {
        string SqlServerSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(SqlServerSyncFrameworkTestCnx), EnvironmentVariableTarget.User);//@"Server=.\sqlexpress;Database=EfMaster;Trusted_Connection=True;";
        string SqlServerSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(SqlServerSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);//@"Server=.\sqlexpress;Database=EfMaster;Trusted_Connection=True;";


        string PostgresSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(PostgresSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
        string PostgresSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(PostgresSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);

        string SQLiteSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(SQLiteSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
        string SQLiteSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(SQLiteSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);

        string MySQLSyncFrameworkTestCnx = Environment.GetEnvironmentVariable(nameof(MySQLSyncFrameworkTestCnx), EnvironmentVariableTarget.User);
        string MySQLSyncFrameworkTestDeltaCnx = Environment.GetEnvironmentVariable(nameof(MySQLSyncFrameworkTestDeltaCnx), EnvironmentVariableTarget.User);

        void DropMysql(string Cnx)
        {
            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(Cnx);
                string DatabaseName = connectionStringParserService.GetPartByName("Database");
                connectionStringParserService.RemovePartByName("Database");

                using (var connection = new MySqlConnection(Cnx))
                {
                    connection.Open();

                    using (var cmd = new MySqlCommand())
                    {
                        cmd.Connection = connection;


                        cmd.CommandText = $"DROP DATABASE IF EXISTS {DatabaseName}";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {

                
            }
          
        }
        void DropPostgres(string Cnx)
        {

            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(Cnx);
                string DatabaseName = connectionStringParserService.GetPartByName("Database");
                connectionStringParserService.RemovePartByName("Database");



                Cnx = connectionStringParserService.GetConnectionString();
                using (var connection = new NpgsqlConnection(Cnx))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = connection;


                        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\"";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {

               
            }
        }
        void DropSqlServer(string Cnx)
        {
           
            try
            {
                ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(Cnx);
                string DatabaseName = connectionStringParserService.GetPartByName("Initial Catalog");
                connectionStringParserService.RemovePartByName("Initial Catalog");

                // Connect to the "master" database to drop the database
                connectionStringParserService.UpdatePartByName("Initial Catalog", "master");
                Cnx = connectionStringParserService.GetConnectionString();

                using (var connection = new SqlConnection(Cnx))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand())
                    {
                        cmd.Connection = connection;

                        cmd.CommandText = $"ALTER DATABASE {DatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {DatabaseName};";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                // Handle exception
            }

        }
        void DropSQLite(string Cnx)
        {
            ConnectionStringParserService connectionStringParserService = new ConnectionStringParserService(Cnx);
            string DatabaseName = connectionStringParserService.GetPartByName("Data Source");
            connectionStringParserService.RemovePartByName("Data Source");
            if(File.Exists(DatabaseName))
            {
                File.Delete(DatabaseName);
            }

           
        }
        TestClientFactory HttpClientFactory;
        [SetUp()]
  
        public override void Setup()
        {

            serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

          

            base.Setup();
            HttpClientFactory = this.GetTestClientFactory();

          

          

            masterContextOptionBuilder.UseSqlServer(SqlServerSyncFrameworkTestCnx);
            node_AContextOptionBuilder.UseSqlite(SQLiteSyncFrameworkTestCnx);
            node_BContextOptionBuilder.UseNpgsql(PostgresSyncFrameworkTestCnx);
            node_CContextOptionBuilder.UseMySql(MySQLSyncFrameworkTestCnx, serverVersion);
        }
        private const string InMemoryConnectionString = "DataSource=:memory:";
        DbContextOptionsBuilder masterContextOptionBuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_AContextOptionBuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_BContextOptionBuilder = new DbContextOptionsBuilder();
        DbContextOptionsBuilder node_CContextOptionBuilder = new DbContextOptionsBuilder();
        MySqlServerVersion serverVersion;
        SqliteConnection GetSQLiteMemoryConnection(string Name)
        {
            var connection = new SqliteConnection($"Data Source={Name};Mode=Memory;");
            connection.Open();
            return connection;
        }
        [Test]
        public async Task MainTest()
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);


            DropPostgres(PostgresSyncFrameworkTestCnx);
            DropPostgres(PostgresSyncFrameworkTestDeltaCnx);

            DropMysql(MySQLSyncFrameworkTestCnx);
            DropMysql(MySQLSyncFrameworkTestDeltaCnx);

            DropSqlServer(SqlServerSyncFrameworkTestCnx);
            DropSqlServer(SqlServerSyncFrameworkTestDeltaCnx);

            DropSQLite(SQLiteSyncFrameworkTestCnx);
            DropSQLite(SQLiteSyncFrameworkTestDeltaCnx);

            var MasterHttpClient = HttpClientFactory.CreateClient("Master");
            var Node_A_HttpClient = HttpClientFactory.CreateClient("Node A");
            var Node_B_HttpClient = HttpClientFactory.CreateClient("Node B");
            var Node_C_HttpClient = HttpClientFactory.CreateClient("Node C");

            ServiceCollection ServiceCollectionMaster = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_A = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_B = new ServiceCollection();
            ServiceCollection ServiceCollectionNode_C = new ServiceCollection();



            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new NpgsqlDeltaGenerator());
            DeltaGenerators.Add(new PomeloMySqlDeltaGenerator(serverVersion));
            DeltaGenerators.Add(new SqliteDeltaGenerator());
            DeltaGenerators.Add(new SqlServerDeltaGenerator());
            DeltaGeneratorBase[] additionalDeltaGenerators = DeltaGenerators.ToArray();





            //ServiceCollectionMaster.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb1"); }, MasterHttpClient, "MemoryDeltaStore1", "Master", additionalDeltaGenerators);

            ////You get register the service in any way you want, this is just an example
            //ServiceCollectionMaster.AddEfSynchronization((options) => { options.UseSqlServer(SqlServerSyncFrameworkTestDeltaCnx); }, MasterHttpClient, "MemoryDeltaStore1", "Master", additionalDeltaGenerators);
            //ServiceCollectionMaster.AddEntityFrameworkSqlServer();

            //you can also use the extension method for specific providers
            ServiceCollectionMaster.AddSyncFrameworkForSqlServer(SqlServerSyncFrameworkTestDeltaCnx, MasterHttpClient, "MemoryDeltaStore1", "Master", additionalDeltaGenerators);



            //ServiceCollectionNode_A.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb2"); }, Node_A_HttpClient, "MemoryDeltaStore1", "Node A", additionalDeltaGenerators);

            ////You get register the service in any way you want, this is just an example
            //ServiceCollectionNode_A.AddEfSynchronization((options) => { options.UseSqlite(SQLiteSyncFrameworkTestDeltaCnx); }, Node_A_HttpClient, "MemoryDeltaStore1", "Node A", additionalDeltaGenerators);
            //ServiceCollectionNode_A.AddEntityFrameworkSqlite();

            //you can also use the extension method for specific providers
            ServiceCollectionNode_A.AddSyncFrameworkForSQLite(SQLiteSyncFrameworkTestDeltaCnx, Node_A_HttpClient, "MemoryDeltaStore1", "Node A", additionalDeltaGenerators);


            //ServiceCollectionNode_B.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb3"); }, Node_B_HttpClient, "MemoryDeltaStore1", "Node B", additionalDeltaGenerators);

            ////You get register the service in any way you want, this is just an example            
            //ServiceCollectionNode_B.AddEfSynchronization((options) => { options.UseNpgsql(PostgresSyncFrameworkTestDeltaCnx); }, Node_B_HttpClient, "MemoryDeltaStore1", "Node B", additionalDeltaGenerators);
            //ServiceCollectionNode_B.AddEntityFrameworkNpgsql();

            //you can also use the extension method for specific providers
            ServiceCollectionNode_B.AddSyncFrameworkForNpgsql(PostgresSyncFrameworkTestDeltaCnx, Node_B_HttpClient, "MemoryDeltaStore1", "Node B", additionalDeltaGenerators);


            ////You get register the service in any way you want, this is just an example   
            //ServiceCollectionNode_C.AddEfSynchronization((options) => { options.UseInMemoryDatabase("MemoryDb4"); }, Node_C_HttpClient, "MemoryDeltaStore1", "Node C", additionalDeltaGenerators);


            //ServiceCollectionNode_C.AddEfSynchronization((options) => { options.UseMySql(MySQLSyncFrameworkTestDeltaCnx, serverVersion); }, Node_C_HttpClient, "MemoryDeltaStore1", "Node C", additionalDeltaGenerators);
            //ServiceCollectionNode_C.AddEntityFrameworkMySql();


            //you can also use the extension method for specific providers
            ServiceCollectionNode_C.AddSyncFrameworkForMysql(MySQLSyncFrameworkTestDeltaCnx,serverVersion, Node_C_HttpClient, "MemoryDeltaStore1", "Node C", additionalDeltaGenerators);


            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            ServiceCollectionMaster.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);
            ServiceCollectionNode_A.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);
            ServiceCollectionNode_B.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);
            ServiceCollectionNode_C.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);



            //ServiceCollectionMaster.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_A.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_B.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));
            //ServiceCollectionNode_C.AddSingleton(typeof(ISequenceService), typeof(MemorySequenceService));


            ServiceCollectionMaster.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            ServiceCollectionNode_A.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            ServiceCollectionNode_B.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            ServiceCollectionNode_C.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));


            var _providerMaster = ServiceCollectionMaster.BuildServiceProvider();
            var _providerNode_A = ServiceCollectionNode_A.BuildServiceProvider();
            var _providerNode_B = ServiceCollectionNode_B.BuildServiceProvider();
            var _providerNode_C = ServiceCollectionNode_C.BuildServiceProvider();

         



            using (var MasterContext = new TestSyncFrameworkDbContext(masterContextOptionBuilder.Options, _providerMaster))
            using (var Node_A_Context = new TestSyncFrameworkDbContext(node_AContextOptionBuilder.Options, _providerNode_A))
            using (var Node_B_Context = new TestSyncFrameworkDbContext(node_BContextOptionBuilder.Options, _providerNode_B))
            using (var Node_C_Context = new TestSyncFrameworkDbContext(node_CContextOptionBuilder.Options, _providerNode_C))
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
                var Master_Push_Result=await MasterContext.PushAsync();
                Assert.IsTrue(Master_Push_Result.Success);


                await Node_A_Context.PullAsync();
                await Node_B_Context.PullAsync();
                await Node_C_Context.PullAsync();

                //Expected 4 for each node
               
                int A_Actual = Node_A_Context.Blogs.Count();
                int B_Actual = Node_B_Context.Blogs.Count();
                int C_Actual = Node_C_Context.Blogs.Count();

                Node_A_Context.Add(GetBlog("Node A Blog", "Post 1", "Post 2"));
                Node_B_Context.Add(GetBlog("Node B Blog", "Post 1", "Post 2"));
                Node_C_Context.Add(GetBlog("Node C Blog", "Post 1", "Post 2"));


                await Node_A_Context.SaveChangesAsync();
                var Node_A_Push= await Node_A_Context.PushAsync();
                Assert.IsTrue(Node_A_Push.Success);

                await Node_B_Context.SaveChangesAsync();
                var Node_B_Push = await Node_B_Context.PushAsync();
                Assert.IsTrue(Node_B_Push.Success);

                await Node_C_Context.SaveChangesAsync();
                var Node_C_Push = await Node_C_Context.PushAsync();
                Assert.IsTrue(Node_C_Push.Success);
                
                
                //Expected 5 for each node



                A_Actual = Node_A_Context.Blogs.Count();
                B_Actual = Node_B_Context.Blogs.Count();
                C_Actual = Node_C_Context.Blogs.Count();

                await MasterContext.PullAsync();

                var blogs = MasterContext.Blogs.ToList();
                var count = blogs.Count;

                Assert.AreEqual(7, count);


                var NodeAFetchedDeltas= await Node_A_Context.FetchAsync();
                var NodeBFetchedDeltas = await Node_B_Context.FetchAsync();
                var NodeCFetchedDeltas = await Node_C_Context.FetchAsync();

                await Node_A_Context.PullAsync();
                await Node_B_Context.PullAsync();
                await Node_C_Context.PullAsync();

                var NodeABlogs = Node_A_Context.Blogs.ToList();
                var NodeBBlogs = Node_B_Context.Blogs.ToList();
                var NodeCBlogs = Node_C_Context.Blogs.ToList();

            

                A_Actual = Node_A_Context.Blogs.Count();
                B_Actual = Node_B_Context.Blogs.Count();
                C_Actual = Node_C_Context.Blogs.Count();

                Assert.AreEqual(7, A_Actual);
                Assert.AreEqual(7, B_Actual);
                Assert.AreEqual(7, C_Actual);

                Node_A_Context.Add(GetBlog("Node A Blog", "Post 2", "Post 3"));
                Node_B_Context.Add(GetBlog("Node B Blog", "Post 2", "Post 3"));
                Node_C_Context.Add(GetBlog("Node C Blog", "Post 2", "Post 3"));

                await Node_A_Context.SaveChangesAsync();
                await Node_A_Context.PushAsync();

                await Node_B_Context.SaveChangesAsync();
                await Node_B_Context.PushAsync();

                await Node_C_Context.SaveChangesAsync();
                await Node_C_Context.PushAsync();

                //Expected 8 for each node
                A_Actual = Node_A_Context.Blogs.Count();
                B_Actual = Node_B_Context.Blogs.Count();
                C_Actual = Node_C_Context.Blogs.Count();

                await MasterContext.PullAsync();

                blogs = MasterContext.Blogs.ToList();
                count = blogs.Count;

                Assert.AreEqual(10, count);
            }



        }
        private static Blog GetBlog(string Name, string Title1, string Title2)
        {
            return new Blog { Name = Name, Posts = { new Post { Title = Title1 }, new Post { Title = Title2 } } };
        }




    }
}