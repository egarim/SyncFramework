using System;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a sync status, which provides an identity and information about the last processed and pushed deltas.
    /// </summary>
    public interface ISyncStatus
    {
        /// <summary>
        /// Gets or sets the identity for synchronization.
        /// </summary>
        string Identity { get; set; }

        /// <summary>
        /// Gets or sets the last processed delta for synchronization.
        /// </summary>
        string LastProcessedDelta { get; set; }

        /// <summary>
        /// Gets or sets the last pushed delta for synchronization.
        /// </summary>
        string LastPushedDelta { get; set; }
    }
}