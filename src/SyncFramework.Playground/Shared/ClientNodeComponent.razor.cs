using BIT.Data.Sync;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using SyncFramework.Playground.EfCore;
using static MudBlazor.CategoryTypes;

namespace SyncFramework.Playground.Shared
{
    public partial class ClientNodeComponent
    {
        [Inject]
        public IDialogService DialogService { get; set; }
        [Parameter]
        public IClientNodeInstance item { get; set; }
        protected async override void OnInitialized()
        {
            base.OnInitialized();
            item.RefreshAction= Refresh;
            await item.Init();
            
           
        }
        public async void OpenDialog(IPerson Person)
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
