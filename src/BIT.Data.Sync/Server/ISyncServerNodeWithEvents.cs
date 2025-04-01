
using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerNodeWithEvents
    {
        event EventHandler<NodeSavingDeltaEventArgs> NodeSavingDelta;
        event EventHandler<NodeSavedDeltaEventArgs> NodeSavedDelta;
        event EventHandler<NodeProcessingDeltaEventArgs> NodeProcessingDelta;
        event EventHandler<NodeProcessedDeltaEventArgs> NodeProcessedDelta;
    }
}
