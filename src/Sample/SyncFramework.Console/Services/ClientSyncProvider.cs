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
    public class ClientSyncProvider : IClientSyncProvider
    {
        public ClientSyncProvider(IDeltaStore deltaStore,
            ISyncFrameworkClient syncFramework,
            ISyncIdentityService identity,
            EFDeltaProcessorHelper deltaProcessorHelper,
            IModificationCommandToCommandDataService modificationCommand)
        {
            DeltaStore = deltaStore;
            SyncFrameworkClient = syncFramework;
            SyncIdentityService = identity;
            DeltaProcessorHelper = deltaProcessorHelper;
            EFSyncFrameworkService = modificationCommand;

        }
        public IDeltaStore DeltaStore { get; set; }

        public IDeltaProcessor DeltaProcessor { get; set; }

        public ISyncFrameworkClient SyncFrameworkClient { get; set; }


        public ISyncIdentityService SyncIdentityService { get; set; }

        public EFDeltaProcessorHelper DeltaProcessorHelper { get; set; }
        public IModificationCommandToCommandDataService EFSyncFrameworkService { get; set; }
    }
}
