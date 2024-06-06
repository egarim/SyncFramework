using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync
{
    /// <summary>
    /// Defines the contract for a delta processor with events, which processes a collection of deltas and raises events before and after processing a delta.
    /// </summary>
    public interface IDeltaProcessorWithEvents
    {
        /// <summary>
        /// Occurs before a delta is process to the store.
        /// </summary>
        event EventHandler<ProcessingDeltaEventArgs> ProcessingDelta;
        /// <summary>
        /// Occurs after a delta has been process to the store.
        /// </summary>
        event EventHandler<BaseProcessedDeltaEventArgs> ProcessedDelta;
    }
}