using BIT.Data.Sync;
using Microsoft.AspNetCore.Components;
using SyncFramework.Playground.EfCore;
using static MudBlazor.CategoryTypes;

namespace SyncFramework.Playground.Shared
{
    public partial class ClientNodeComponent
    {
        [Parameter]
        public IClientNodeInstance item { get; set; }
        protected async override void OnInitialized()
        {
            base.OnInitialized();
            item.RefreshAction= Refresh;
            await item.Init();
            
           
        }
        void Refresh()
        {
            this.StateHasChanged();
        }
    }
}
