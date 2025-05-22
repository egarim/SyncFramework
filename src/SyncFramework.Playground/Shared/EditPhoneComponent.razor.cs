using Microsoft.AspNetCore.Components;
using MudBlazor;
using SyncFramework.Playground.Components.Interfaces;

namespace SyncFramework.Playground.Shared
{
    public partial class EditPhoneComponent
    {
        public MudForm form { get; set; }
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

        [Parameter] public IPhoneNumber Phone { get; set; }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void UpdatePhone()
        {
            //In a real world scenario this bool would probably be a service to delete the item from api/database
            Snackbar.Add("Phone updated", Severity.Success);
            MudDialog.Close(DialogResult.Ok(Phone));
        }
    }
}
