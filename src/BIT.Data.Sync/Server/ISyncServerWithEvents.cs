
using System;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerWithEvents
    {
        event EventHandler<SyncServerSavingDeltaEventArgs> SavingDelta;
        event EventHandler<SyncServerSavedDeltaEventArgs> SavedDelta;
        event EventHandler<SyncServerProcessingDeltaEventArgs> ProcessingDelta;
        event EventHandler<SyncServerProcessDeltaBaseEventArgs> ProcessedDelta;

    }
}
