using System;

namespace BIT.Data.Sync
{
    public class SyncIdentityService : ISyncIdentityService
    {

        public SyncIdentityService()
        {

        }
        public SyncIdentityService(string identity)
        {
            Identity = identity;
        }

        public string Identity { get; set; }
    }
}
