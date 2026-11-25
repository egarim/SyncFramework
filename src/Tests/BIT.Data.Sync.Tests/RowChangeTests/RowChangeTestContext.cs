using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.Imp;
using BIT.EfCore.Sync;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace BIT.Data.Sync.Tests.RowChangeTests
{
    public class Blog
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    /// <summary>DateOnly is not a row change value type, so capturing a change to it must be refused.</summary>
    public class Holiday
    {
        public int Id { get; set; }

        public DateOnly Day { get; set; }
    }

    public class RowChangeTestContext : SyncFrameworkDbContext
    {
        public RowChangeTestContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options, serviceProvider)
        {
        }

        public DbSet<Blog> Blogs { get; set; }

        public DbSet<Holiday> Holidays { get; set; }
    }

    /// <summary>
    /// A <see cref="RowChangeTestContext"/> on a SQLite in-memory database, saving its deltas to a <see cref="MemoryDeltaStore"/>.
    /// </summary>
    public sealed class SqliteMemoryClient : IDisposable
    {
        public const string BlogsTable = "Blogs";
        public const string BlogsSchema = "CREATE TABLE \"Blogs\" (\"Id\" INTEGER NOT NULL CONSTRAINT \"PK_Blogs\" PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT NULL);";

        private SqliteMemoryClient()
        {
        }

        public SqliteConnection Connection { get; private set; }
        public SqliteConnection DeltaConnection { get; private set; }
        public MemoryDeltaStore DeltaStore { get; private set; }
        public RowChangeTestContext Context { get; private set; }

        public static SqliteMemoryClient Create()
        {
            var client = new SqliteMemoryClient();
            client.Connection = OpenMemoryDatabase();
            // The EF delta store is replaced by the memory store below; its connection string is never opened.
            client.DeltaConnection = OpenMemoryDatabase();
            client.DeltaStore = new MemoryDeltaStore();

            var services = new ServiceCollection();
            services.AddSyncFrameworkForSQLite(client.DeltaConnection.ConnectionString,
                new HttpClient { BaseAddress = new Uri("http://localhost/sync/") }, "Node", "Client",
                new SqliteDeltaGenerator());
            services.AddSingleton<IDeltaStore>(client.DeltaStore);
            services.AddSingleton<ISequenceService>(new MemorySequenceService(new YearSequencePrefixStrategy()));

            var options = new DbContextOptionsBuilder().UseSqlite(client.Connection).Options;
            client.Context = new RowChangeTestContext(options, services.BuildServiceProvider());
            client.Context.Database.EnsureCreated();
            return client;
        }

        /// <summary>An empty SQLite in-memory database with the same Blogs table, for applying row changes to.</summary>
        public static SqliteConnection CreateTarget()
        {
            var connection = OpenMemoryDatabase();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = BlogsSchema;
                command.ExecuteNonQuery();
            }
            return connection;
        }

        public static SqliteConnection OpenMemoryDatabase()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        public void Dispose()
        {
            Context?.Dispose();
            Connection?.Dispose();
            DeltaConnection?.Dispose();
        }
    }
}
