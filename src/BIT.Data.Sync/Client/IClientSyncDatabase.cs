
namespace BIT.Data.Sync.Client
{
    public interface IClientSyncDatabase
    {
        IDeltaProcessor DeltaProcessor { get; }
        IDeltaStore DeltaStore { get; }
        ISyncFrameworkClient SyncFrameworkClient { get; }
        string Identity { get;  }

    }
}