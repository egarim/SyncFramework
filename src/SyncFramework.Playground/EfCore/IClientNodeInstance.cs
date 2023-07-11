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
        List<PhoneNumber> PhoneNumbers { get; set; }
        List<IPerson> People { get; set; }
        IPerson SelectedPerson { get; set; }

        Task AddPerson(string personName);
        void DownloadFile();
        Task Pull();
        Task Push();
        void SelectedPersonChange(IPerson Person);
    }
}