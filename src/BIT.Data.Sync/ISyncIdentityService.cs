using System;
using System.Collections.Generic;
using System.Text;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a sync identity service, which provides an identity for synchronization.
    /// </summary>
    public interface ISyncIdentityService
    {
        /// <summary>
        /// Gets or sets the identity for synchronization.
        /// </summary>
        string Identity { get; set; }
    }
}
