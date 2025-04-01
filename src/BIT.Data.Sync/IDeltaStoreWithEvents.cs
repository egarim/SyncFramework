using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a delta store with events, which provides methods for saving and retrieving deltas and raises events before and after saving a delta.
    /// </summary>
    public interface IDeltaStoreWithEvents
    {
        /// <summary>
        /// Occurs before a delta is saved to the store.
        /// </summary>
        event EventHandler<SavingDeltaEventArgs> SavingDelta;
        /// <summary>
        /// Occurs after a delta has been saved to the store.
        /// </summary>
        event EventHandler<SavedDeltaEventArgs> SavedDelta;
    }
}