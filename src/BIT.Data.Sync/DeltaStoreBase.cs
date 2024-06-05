
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync
{
    public abstract class DeltaStoreBase : IDeltaStore, IDeltaStoreWithEvents
    {
        protected ISequenceService sequenceService;

        public event EventHandler<SavingDeltaEventArgs> SavingDelta;
        public event EventHandler<SavedDeltaEventArgs> SavedDelta;
       
        protected virtual void OnSavingDelta(SavingDeltaEventArgs e)
        {
            
            SavingDelta?.Invoke(this, e);
        }
        protected virtual void OnSavedDelta(SavedDeltaEventArgs e)
        {
            SavedDelta?.Invoke(this, e);
        }
        public ISequenceService SequenceService => sequenceService;

        public DeltaStoreBase(ISequenceService sequenceService)
        {
            this.sequenceService = sequenceService;
        }
      
        protected virtual string GuardStartIndex(string startIndex)
        {
            if (startIndex == null)
            {
                return "";
            }
            else
            {
                return startIndex;
            }
        }
        protected virtual async Task SetDeltaIndex(IDelta delta)
        {
            delta.Index = await sequenceService.GenerateNextSequenceAsync();
        }
        public abstract Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default);

        public abstract Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken = default);
        public abstract Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default);
        public abstract Task SetLastProcessedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default);

        public abstract Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default);
        public abstract Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken = default);

        public abstract Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default);
        public abstract Task SetLastPushedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default);

        public abstract Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken=default);
        public abstract Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken=default);

        public abstract Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken=default);

    

    
    }
}