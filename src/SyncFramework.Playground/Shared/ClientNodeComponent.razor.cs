using BIT.Data.Sync;
using Microsoft.AspNetCore.Components;
using SyncFramework.Playground.EfCore;

namespace SyncFramework.Playground.Shared
{
    public partial class ClientNodeComponent
    {
        [Parameter]
        public IClientNodeInstance item { get; set; }
    }
}
