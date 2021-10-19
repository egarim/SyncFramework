
namespace BIT.Data.Sync.Client
{
    public interface ISyncClientNode
    {
        IDeltaProcessor DeltaProcessor { get; }
        IDeltaStore DeltaStore { get; }
        ISyncFrameworkClient SyncFrameworkClient { get; }
        string Identity { get;  }

    }
}