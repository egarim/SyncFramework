using BIT.Data.Sync;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SyncFramework.Playground.EfCore;
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
        protected async override void OnInitialized()
        {
            base.OnInitialized();
            item.RefreshAction= Refresh;
            item.ShowMessage = ShowMessage;
            await item.Init();
            
           
        }
        public void ShowMessage(string Message)
        {
            Snackbar.Add(Message, Severity.Success);
        }
        public async void EditPhone(IPhoneNumber phone)
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
                //In a real world scenario we would reload the data from the source here since we "removed" it in the dialog already.
                //Guid.TryParse(result.Data.ToString(), out Guid deletedServer);
                //Servers.RemoveAll(item => item.Id == deletedServer);
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
        public async void EditPerson(IPerson Person)
        {
            //var options = new DialogOptions { CloseOnEscapeKey = true, Position = DialogPosition.Center, CloseButton=true };
            //DialogService.Show<EditPersonComponent>("Edit Person", options);

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
                //In a real world scenario we would reload the data from the source here since we "removed" it in the dialog already.
                //Guid.TryParse(result.Data.ToString(), out Guid deletedServer);
                //Servers.RemoveAll(item => item.Id == deletedServer);
            }
        }
        void Refresh()
        {
            this.StateHasChanged();

        }
    }
}
