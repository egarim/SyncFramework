
using System;
using BIT.Data.Sync.EventArgs;

namespace BIT.Data.Sync.Server
{
    public interface ISyncServerWithEvents
    {
        event EventHandler<ServerSavingDeltaEventArgs> ServerSavingDelta;
        event EventHandler<ServerSavedDeltaEventArgs> ServerSavedDelta;
        event EventHandler<ServerProcessingDeltaEventArgs> ServerProcessingDelta;
        event EventHandler<ServerProcessedDeltaEventArgs> ServerProcessedDelta;

    }
}
