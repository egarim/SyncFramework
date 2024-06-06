
using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerWithEvents
    {
        event EventHandler<ServerSavingDeltaEventArgs> SavingDelta;
        event EventHandler<ServerSavedDeltaEventArgs> SavedDelta;
        event EventHandler<ServerProcessingDeltaEventArgs> ProcessingDelta;
        event EventHandler<ServerProcessedDeltaEventArgs> ProcessedDelta;

    }
}
