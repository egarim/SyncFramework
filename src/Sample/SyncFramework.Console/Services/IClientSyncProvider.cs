using BIT.Data.Sync;
using BIT.Data.Sync.Client;
using BIT.EfCore.Sync;
using SyncFramework.ConsoleApp.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Services
{
    public interface IClientSyncProvider
    {
        IDeltaStore DeltaStore { get; }
        IDeltaProcessor DeltaProcessor { get; }
        ISyncFrameworkClient SyncFrameworkClient { get; }
        ISyncIdentityService SyncIdentityService { get; }
        EFDeltaProcessorHelper DeltaProcessorHelper { get; }
        IModificationCommandToCommandDataService EFSyncFrameworkService { get; }
    }
}
