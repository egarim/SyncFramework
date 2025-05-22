using Microsoft.AspNetCore.Components;
using MudBlazor;
using SyncFramework.Playground.Components.Interfaces;

namespace SyncFramework.Playground.Shared
{
    public partial class EditPersonComponent
    {
        public MudForm form { get; set; }
        [CascadingParameter] IMudDialogInstance MudDialog { get; set; }

        [Parameter] public IPerson Person { get; set; }

        private void Cancel()
        {
            MudDialog.Cancel();
        }

        private void UpdatePerson()
        {
            //In a real world scenario this bool would probably be a service to delete the item from api/database
            Snackbar.Add("Person updated", Severity.Success);
            MudDialog.Close(DialogResult.Ok(Person));
        }
    }
}
