using System;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Provides an implementation for a sync identity service, which includes an identity for synchronization.
    /// </summary>
    public class SyncIdentityService : ISyncIdentityService
    {
        /// <summary>
        /// Initializes a new instance of the SyncIdentityService class.
        /// </summary>
        public SyncIdentityService()
        {

        }

        /// <summary>
        /// Initializes a new instance of the SyncIdentityService class with the specified identity.
        /// </summary>
        /// <param name="identity">The identity for synchronization.</param>
        public SyncIdentityService(string identity)
        {
            Identity = identity;
        }

        /// <summary>
        /// Gets or sets the identity for synchronization.
        /// </summary>
        public string Identity { get; set; }
    }
}
