using BIT.Data.Sync;
using BlazorComponentBus;
using Microsoft.JSInterop;

namespace SyncFramework.Playground.EfCore
{
    public interface IClientNodeInstance
    {
        ComponentBus Bus { get; set; }
   
        int DeltaCount { get; }
        string Id { get; set; }
        IJSRuntime js { get; set; }
        string PersonName { get; set; }
        List<IPhoneNumber> PhoneNumbers { get; set; }
        List<IPerson> People { get; set; }
        IPerson SelectedPerson { get; set; }
        bool IsLoading { get; set; }
        public Action RefreshAction { get; set; }
        public Dictionary<IDelta,string> Deltas { get; }
        public Action<string> ShowMessage { get; set; }
        Task AddPerson(string personName);
        public Task RemovePerson(IPerson person);
        public Task UpdatePerson(IPerson person);
        public Task RemovePhone(IPhoneNumber person);
        public Task UpdatePhone(IPhoneNumber person);
        public Task PurgeDeltas();
        public Task ReloadData();
        void DownloadFile();
        Task Pull();
        Task Push();
  
        public Task Init();
        void SelectedPersonChange(IPerson Person);
    }
}