using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SyncFramework.Playground.EfCore;
using SyncFramework.Playground.Shared;
using System;
using System.Net.Http;
using System.Security.Principal;
using static MudBlazor.CategoryTypes;

namespace SyncFramework.Playground.Pages
{
    public partial class EfCore
    {
        public IQueryable<ClientNodeInstance> ClientNodes
        {
            get
            {
                return clientNodes.AsQueryable();
            }
           
        }
        [Inject]
        private IJSRuntime js { get; set; }
        [Inject]
        public DeltaGeneratorBase[] deltaGeneratorBases { get; set; }
        public IDeltaStore ServerDeltaStore { get; set; }
        public string NodeId { get; set; }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            ServerDeltaStore = new MemoryDeltaStore();
            NodeId = "MainServer";

            

        }
        public byte[] GetResource(string filename)
        {


            using (var stream = this.GetType().Assembly.
                       GetManifestResourceStream("SyncFramework.Playground." + filename))
            {
                var Ms = new MemoryStream();
                stream.CopyTo(Ms);
                return Ms.ToArray();
            }

        }
        SyncServerComponent serverComponent;
        private List<ClientNodeInstance> clientNodes = new List<ClientNodeInstance>();

        private int NodeCount;
        private async void AddClientNode()
        {
            NodeCount++;
            string DbName = $"ClientNode_{NodeCount}";

            File.WriteAllBytes($"{DbName}_Deltas.db", GetResource("mydb.db"));

            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddSyncFrameworkForSQLite($"Data Source={DbName}_Deltas.db", this.serverComponent.HttpClient, this.serverComponent.NodeId, DbName, deltaGeneratorBases);

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            serviceCollection.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);

            serviceCollection.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            var ServiceProvider = serviceCollection.BuildServiceProvider();

            DbContextOptionsBuilder OptionsBuilder = new DbContextOptionsBuilder();
            OptionsBuilder.UseSqlite($"Data Source={DbName}_Data.db");

            File.WriteAllBytes($"{DbName}_Data.db", GetResource("mydb.db"));

            var DbContextInstance = new BlogsDbContext(OptionsBuilder.Options, ServiceProvider);
            await DbContextInstance.Database.EnsureDeletedAsync();
            await DbContextInstance.Database.EnsureCreatedAsync();


            Blog SqlServerBlog = GetBlog("SqlServer blog", "EF Core 3.1!", "EF Core 5.0!");
            Blog SqliteBlog = GetBlog("Sqlite blog", "EF Core 3.1!", "EF Core 5.0!");
            Blog NpgsqlBlog = GetBlog("Npgsql blog", "EF Core 3.1!", "EF Core 5.0!");
            Blog PomeloMySqlBlog = GetBlog("Pomelo MySql blog", "EF Core 3.1!", "EF Core 5.0!");

            DbContextInstance.Add(SqlServerBlog);
            DbContextInstance.Add(SqliteBlog);
            DbContextInstance.Add(NpgsqlBlog);
            DbContextInstance.Add(PomeloMySqlBlog);
            await DbContextInstance.SaveChangesAsync();
            this.clientNodes.Add(new ClientNodeInstance { Id = DbName, DbContext = DbContextInstance, js=this.js });
         
            //await DbContextInstance.PushAsync();
        }
        private static Blog GetBlog(string Name, string Title1, string Title2)
        {
            return new Blog { Name = Name, Posts = { new Post { Title = Title1 }, new Post { Title = Title2 } } };
        }
        async Task Delete(Blog b,BlogsDbContext d)
        {
            d.Remove(b);
            await d.SaveChangesAsync();
           
        }
        void Push(ClientNodeInstance p)
        {
           
        }

        void Pull(ClientNodeInstance p)
        {
          
        }
     
        private void PrcdBtnClick(ClientNodeInstance x)
        {
            //ElementsList = ElementsList.Where(u => u.ID != x.ID).ToList();
        }
    }
}
