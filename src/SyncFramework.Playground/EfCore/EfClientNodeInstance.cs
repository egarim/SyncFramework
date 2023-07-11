using BIT.Data.Sync.Imp;
using BIT.Data.Sync;
using BIT.EfCore.Sync;
using BlazorComponentBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text;
using Microsoft.Data.Sqlite;
using Bogus;

namespace SyncFramework.Playground.EfCore
{
    public class EfClientNodeInstance : IClientNodeInstance
    {
        string DbName;
        HttpClient httpClient;
        string ServerNodeId;
        DeltaGeneratorBase[] deltaGeneratorBases;
        bool GenerateRandomData;
        public EfClientNodeInstance(IJSRuntime js, string id, ComponentBus bus,string DbName,HttpClient httpClient,string ServerNodeId, DeltaGeneratorBase[] deltaGeneratorBases,bool GenerateRandomData)
        {
            this.js = js;
            Id = id;
            Bus = bus;
            this.DbName = DbName;
            this.httpClient = httpClient;
            this.ServerNodeId = ServerNodeId;
            this.deltaGeneratorBases = deltaGeneratorBases;
            this.GenerateRandomData = GenerateRandomData;
            this.IsLoading = true;
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
        public async Task Init()
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            ServiceCollection serviceCollection = new ServiceCollection();
            string sQliteDeltaStoreConnectionString = $"Data Source={DbName}_Deltas.db";
            CreateDatabaseConnection(sQliteDeltaStoreConnectionString);

            serviceCollection.AddSyncFrameworkForSQLite(sQliteDeltaStoreConnectionString, this.httpClient, this.ServerNodeId, DbName, deltaGeneratorBases);

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            serviceCollection.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);

            serviceCollection.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));

            var ServiceProvider = serviceCollection.BuildServiceProvider();

            DbContextOptionsBuilder OptionsBuilder = new DbContextOptionsBuilder();
            string DataConnectionString = $"Data Source={DbName}_Data.db";

            OptionsBuilder.UseSqlite(DataConnectionString);

            this.DbContext = new ContactsDbContext(OptionsBuilder.Options, ServiceProvider);
            //Delete the database if it exists
            await DbContext.Database.EnsureDeletedAsync();
            //Create the database with the sqlite connection
            CreateDatabaseConnection(DataConnectionString);
            await DbContext.Database.EnsureCreatedAsync();

            if (GenerateRandomData)
            {
                var Persons = GetPerson(3);
                DbContext.AddRange(Persons);
                await DbContext.SaveChangesAsync();
            }

            await ReloadPeople();

            IsLoading = false;
            this.RefreshAction?.Invoke();
        }

        private async Task ReloadPeople()
        {
            People.Clear();
            List<Person> people = await DbContext.Persons.ToListAsync();
            foreach (Person person in people)
            {
                People.Add(person);
            }
            await UpdateDeltaCount();
        }
     
        private async Task UpdateDeltaCount()
        {
            this.DeltaCount = await DbContext.DeltaStore.GetDeltaCountAsync("", this.Id, default);
        }

        public IJSRuntime js { get; set; }
        public string Id { get; set; }
        public ContactsDbContext DbContext
        {
            get
            {
                return dbContext;
            }
            set
            {
                dbContext = value;

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
                var Phones = TestPhones.Generate(3);
                foreach (var item in Phones)
                {
                    person.PhoneNumbers.Add(item);
                }

            }
            return user;
        }
        public string PersonName { get; set; }
        public int DeltaCount { get; set; }
        public BlazorComponentBus.ComponentBus Bus { get; set; }
        IPerson selectedPerson;
        private ContactsDbContext dbContext;

        public IPerson SelectedPerson
        {
            get
            {
                return selectedPerson;
            }

            set
            {
                selectedPerson = value;
                SelectedPersonChange(selectedPerson);
            }
        }
        public async Task AddPerson(string personName)
        {
            var PersonFullName = personName.Split(' ');
            var LastName = PersonFullName.Length > 1 ? PersonFullName[1] : string.Empty;
            DbContext.Persons.Add(new Person { FirstName = PersonFullName[0], LastName = LastName });
            await DbContext.SaveChangesAsync();
            this.PersonName = string.Empty;
            await UpdateDeltaCount();
        }
        public List<IPhoneNumber> PhoneNumbers { get; set; } =new List<IPhoneNumber>();
        public List<IPerson> People { get; set; }=new List<IPerson>();
        public bool IsLoading { get; set; }

        public void SelectedPersonChange(IPerson Person)
        {
            var NumbersFromDb = this.dbContext.Phones.Where(x => x.Person.Id == Person.Id).ToList();
            PhoneNumbers.Clear();
            foreach (PhoneNumber phoneNumber in NumbersFromDb)
            {
                PhoneNumbers.Add(phoneNumber);
            }

        }

        public async void DownloadFile()
        {
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

            string DataFileName = $"{Id}_Data.db";
            string DeltaFileName = $"{Id}_Deltas.db";
            var DataDbBytes = File.ReadAllBytes(DataFileName);
            var DeltasDbBytes = File.ReadAllBytes(DeltaFileName);
            files.Add(DataFileName, DataDbBytes);
            files.Add(DeltaFileName, DeltasDbBytes);
            var zipBytes = FileUtil.CreateZipFromByteArrays(files);
            await FileUtil.SaveAs(js, $"{Id}.zip", zipBytes);
        }
        public async Task Push()
        {

            await DbContext.PushAsync();
            await Bus.Publish(new object());
            ShowMessage($"{this.Id} Deltas Pushed");


        }
        public Action RefreshAction { get; set; }
        public Action<string> ShowMessage { get; set; }

        public async Task Pull()
        {
            await DbContext.PullAsync();
            await ReloadPeople();
            this.RefreshAction?.Invoke();
            ShowMessage($"{this.Id} Deltas Pulled");

        }

        public Task RemovePerson(IPerson person)
        {
            throw new NotImplementedException();
        }

        public async Task UpdatePerson(IPerson person)
        {
            var PersonToUpdate= await this.DbContext.Persons.FindAsync(person.Id);
            PersonToUpdate.FirstName = person.FirstName;
            PersonToUpdate.LastName = person.LastName;
               
            //this.dbContext.Update(person as Person);
            await this.DbContext.SaveChangesAsync();
            await this.ReloadPeople();
            this.RefreshAction?.Invoke();
        }

        public Task RemovePhone(IPhoneNumber person)
        {
            throw new NotImplementedException();
        }

        public async Task UpdatePhone(IPhoneNumber Phone)
        {
            var PhoneToUpdate = await this.DbContext.Phones.FindAsync(Phone.Id);
            PhoneToUpdate.Number = Phone.Number;
        

           
            await this.DbContext.SaveChangesAsync();
            SelectedPersonChange(PhoneToUpdate.Person);
            await UpdateDeltaCount();
            this.RefreshAction?.Invoke();
        }

        public async Task ReloadData()
        {
            await ReloadPeople();
            this.RefreshAction?.Invoke();
            ShowMessage($"{this.Id} Refreshed");
        }
    }
}
