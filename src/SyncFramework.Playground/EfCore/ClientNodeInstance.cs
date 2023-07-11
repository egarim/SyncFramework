using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Text;

namespace SyncFramework.Playground.EfCore
{
    public class ClientNodeInstance : IClientNodeInstance
    {
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

        public string PersonName { get; set; }
        public int DeltaCount
        {
            get
            {
                var CountTask = DbContext.DeltaStore.GetDeltaCountAsync("", this.Id, default);
                CountTask.Wait();
                return CountTask.Result;
            }


        }
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
        }
        public List<PhoneNumber> PhoneNumbers { get; set; }
        public List<IPerson> People { get; set; }
        public void SelectedPersonChange(IPerson Person)
        {

            PhoneNumbers = this.dbContext.Phones.Where(x => x.Person.Id == Person.Id).ToList();
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
        }
        public async Task Pull()
        {
            await DbContext.PullAsync();
        }
    }
}
