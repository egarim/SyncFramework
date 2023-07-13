using BIT.Data.Sync;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SyncFramework.Playground.EfCore;
using System;
using System.Text;
using static MudBlazor.CategoryTypes;

namespace SyncFramework.Playground.Shared
{
    public partial class ClientNodeComponent
    {
        [Inject]
        public IDialogService DialogService { get; set; }
        [Parameter]
        public IClientNodeInstance item { get; set; }
        [Inject]
        public IJSRuntime js { get; set; }
        public MudBlazor.Color Color { get; set; }
        protected async override void OnInitialized()
        {
            base.OnInitialized();
            item.RefreshAction= Refresh;
            item.ShowMessage = ShowMessage;
            this.Color=GetRandomEnumValue<MudBlazor.Color>();
            await item.Init();
            
           
        }
        public void ShowMessage(string Message)
        {
            Snackbar.Add(Message, Severity.Success);
        }
        private static readonly Random random = new Random();
        public static T GetRandomEnumValue<T>()
        {
            var values = Enum.GetValues(typeof(T)).Cast<T>();
            return values.ElementAt(random.Next(values.Count()));
        }
        
        public async Task EditPhone(IPhoneNumber phone)
        {
            
            var parameters = new DialogParameters<IPhoneNumber>
            {
                { "Phone", phone }
            };

            var dialog = await DialogService.ShowAsync<EditPhoneComponent>("Edit Phone", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var UpdatedPhoneNumber = result.Data as IPhoneNumber;
                await this.item.UpdatePhone(UpdatedPhoneNumber);
             
            }
        }
        public async void PreviewDelta(string Delta)
        {
            Delta = CleanDelta(Delta);
            DialogOptions fullScreen = new DialogOptions() { FullScreen = true, CloseButton = true };
            var parameters = new DialogParameters<IPhoneNumber>
            {
                { "Content", Delta }
            };

            var dialog = await DialogService.ShowAsync<DeltaPreviewComponent>("Delta", parameters, fullScreen);

        }

        private static string CleanDelta(string Delta)
        {
            Delta = Delta.Replace("\\u0022", "\"").Replace("\\u0060", "\'").Replace("\\n", "");
            return Delta;
        }

        public async void DownloadDelta(KeyValuePair<IDelta,string> Delta)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(CleanDelta(Delta.Value));
            await FileUtil.SaveAs(js, $"{Delta.Key.Index}.json", byteArray);

        }
        public async Task EditPerson(IPerson Person)
        {
           
            var parameters = new DialogParameters<IPerson>
            {
                { "Person", Person }
            };

            var dialog = await DialogService.ShowAsync<EditPersonComponent>("Delete Server", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                var UpdatedPerson= result.Data as IPerson;
                await this.item.UpdatePerson(UpdatedPerson);
              
            }
        }
        void Refresh()
        {
            this.StateHasChanged();

        }
        public async Task AddPerson()
        {
            await EditPerson(new Person());
           
        }
        public async Task AddPhone()
        {

            await EditPhone(new PhoneNumber() { Person=this.item.SelectedPerson as Person });

        }
    }
}
