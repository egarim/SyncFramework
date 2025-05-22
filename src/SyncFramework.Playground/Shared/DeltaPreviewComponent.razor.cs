using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace SyncFramework.Playground.Shared
{
    public partial class DeltaPreviewComponent
    {
    
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

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
