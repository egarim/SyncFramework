using Microsoft.AspNetCore.Components;
using MudBlazor;
using SyncFramework.Playground.EfCore;

namespace SyncFramework.Playground.Shared
{
    public partial class DeltaPreviewComponent
    {
    
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public string Content { get; set; }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void UpdatePerson()
        {
            
            MudDialog.Close();
        }
    }
}
