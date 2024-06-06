
using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerNodeWithEvents
    {
        event EventHandler<NodeSavingDeltaEventArgs> SavingDelta;
        event EventHandler<NodeSavedDeltaEventArgs> SavedDelta;
        event EventHandler<NodeProcessingDeltaEventArgs> ProcessingDelta;
        event EventHandler<NodeProcessDeltaBaseEventArgs> ProcessedDelta;
    }
}
