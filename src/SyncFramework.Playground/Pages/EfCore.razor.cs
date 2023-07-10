using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using BlazorComponentBus;
using Bogus;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using SyncFramework.Playground.EfCore;
using SyncFramework.Playground.Shared;
using System;
using System.Net.Http;
using System.Security.Principal;
using static MudBlazor.CategoryTypes;
using Person = SyncFramework.Playground.EfCore.Person;

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
        BlazorComponentBus.ComponentBus Bus { get; set; }

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
            //Subscribe Component to Specific Message
            Bus.Subscribe<object>(RefreshDeltaCount);
            Randomizer.Seed = new Random(8675309);

        }
        private async void RefreshDeltaCount(MessageArgs args)
        {
            var message = args.GetMessage<object>();
            DeltaCount = await this.ServerDeltaStore.GetDeltaCountAsync("", NodeId, default);
           
            this.StateHasChanged();
           
        }
        public bool GenerateRandomData { get; set; } = true;
        SyncServerComponent serverComponent;
        private List<ClientNodeInstance> clientNodes = new List<ClientNodeInstance>();

        private int NodeCount;
        private async void AddClientNode()
        {
            NodeCount++;
            string DbName = $"ClientNode_{NodeCount}";

           

            ServiceCollection serviceCollection = new ServiceCollection();
            string sQliteDeltaStoreConnectionString = $"Data Source={DbName}_Deltas.db";
            CreateDatabaseConnection(sQliteDeltaStoreConnectionString);

            serviceCollection.AddSyncFrameworkForSQLite(sQliteDeltaStoreConnectionString, serverComponent.HttpClient, serverComponent.NodeId, DbName, deltaGeneratorBases);

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            serviceCollection.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);

            serviceCollection.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            var ServiceProvider = serviceCollection.BuildServiceProvider();

            DbContextOptionsBuilder OptionsBuilder = new DbContextOptionsBuilder();
            string DataConnectionString = $"Data Source={DbName}_Data.db";
            
            OptionsBuilder.UseSqlite(DataConnectionString);

            var DbContextInstance = new ContactsDbContext(OptionsBuilder.Options, ServiceProvider);
            //Delete the database if it exists
            await DbContextInstance.Database.EnsureDeletedAsync();
            //Create the database with the sqlite connection
            CreateDatabaseConnection(DataConnectionString);
            await DbContextInstance.Database.EnsureCreatedAsync();

            if(GenerateRandomData)
            {
                var Persons = GetPerson(5);
                DbContextInstance.AddRange(Persons);
                await DbContextInstance.SaveChangesAsync();
            }
           
            this.clientNodes.Add(new ClientNodeInstance { Id = DbName, DbContext = DbContextInstance, js = this.js,Bus=this.Bus });

            //await DbContextInstance.PushAsync();
        }

        /// <summary>
        /// This method is used to create the database,DbContextInstance.Database.EnsureCreatedAsync does not create a physical database
        /// </summary>
        /// <param name="DatabaseConnectionString"></param>
        private static void CreateDatabaseConnection(string DatabaseConnectionString)
        {
            using (var connection = new SqliteConnection(DatabaseConnectionString))
            {
                connection.Open();  //  <== The database file is created here.

                

            }
        }

        private static IEnumerable<Person> GetPerson(int Count)
        {
            var testUsers = new Faker<Person>()
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName());
            var user = testUsers.Generate(Count);

            var TestPhones = new Faker<PhoneNumber>()
            .RuleFor(u => u.Number, f => f.Phone.PhoneNumber());
            
        

            foreach (Person person in user)
            {
                var Phones=TestPhones.Generate(3);
                foreach (var item in Phones)
                {
                    person.PhoneNumbers.Add(item);
                }
                
            }
            return user;
        }

        public int DeltaCount { get; set; }
    }
}
