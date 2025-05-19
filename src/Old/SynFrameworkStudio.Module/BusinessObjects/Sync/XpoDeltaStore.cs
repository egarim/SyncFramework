using BIT.Data.Sync;
using BIT.Data.Sync.EventArgs;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{

    public class XpoDeltaStore : DeltaStoreBase
    {
        IObjectSpaceProvider Provider;
        public XpoDeltaStore(ISequenceService sequenceService, IObjectSpaceProvider objectSpace) : base(sequenceService)
        {
            Provider = objectSpace;
        }

        public override async Task<IDelta> GetDeltaAsync(string deltaId, CancellationToken cancellationToken)
        {
            var delta = Provider.CreateObjectSpace().FindObject<XpoDeltaRecord>(CriteriaOperator.Parse("DeltaId = ?", deltaId));
            return delta;
        }

        public override async Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            var guardedStartIndex = GuardStartIndex(startIndex);
            return Provider.CreateObjectSpace().GetObjects<XpoDeltaRecord>(
                CriteriaOperator.Parse("Identity = ? AND Index > ?", identity, guardedStartIndex)).Count;
        }

        public override async Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default)
        {
            var guardedStartIndex = GuardStartIndex(startIndex);
            var deltas = Provider.CreateObjectSpace().GetObjects<XpoDeltaRecord>(
                CriteriaOperator.Parse("Index > ?", guardedStartIndex));
            return deltas.Cast<IDelta>();
        }

        public override async Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            var guardedStartIndex = GuardStartIndex(startIndex);
            var deltas = Provider.CreateObjectSpace().GetObjects<XpoDeltaRecord>(
                CriteriaOperator.Parse("Identity = ? AND Index > ?", identity, guardedStartIndex));
            return deltas.Cast<IDelta>();
        }

        public override async Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            var guardedStartIndex = GuardStartIndex(startIndex);
            var deltas = Provider.CreateObjectSpace().GetObjects<XpoDeltaRecord>(
                CriteriaOperator.Parse("Identity != ? AND Index > ?", identity, guardedStartIndex));
            return deltas.Cast<IDelta>();
        }

        public override async Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            var state = GetOrCreateDeltaState(identity, Provider.CreateObjectSpace());
            return state.LastProcessedDelta;
        }

        public override async Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            var state = GetOrCreateDeltaState(identity, Provider.CreateObjectSpace());
            return state.LastPushedDelta;
        }

        public override async Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        {
            IObjectSpace objectSpace = Provider.CreateObjectSpace();
            var deltas = objectSpace.GetObjects<XpoDeltaRecord>(
                CriteriaOperator.Parse("Identity = ?", identity));

            foreach (var delta in deltas)
            {
                objectSpace.Delete(delta);
            }

            objectSpace.CommitChanges();
        }

        public override async Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        {
            var state = GetOrCreateDeltaState(identity, Provider.CreateObjectSpace());
            var firstIndex = await SequenceService.GetFirstIndexValue();

            state.LastProcessedDelta = firstIndex;
            state.LastPushedDelta = firstIndex;

            Provider.CreateObjectSpace().CommitChanges();
        }

        public override async Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        {
            var os= Provider.CreateObjectSpace();
            foreach (var delta in deltas)
            {
                var savingArgs = new SavingDeltaEventArgs(delta);
                OnSavingDelta(savingArgs);

                if (!savingArgs.CustomHandled)
                {
                    // Set the delta index if it doesn't have one
                    if (string.IsNullOrEmpty(delta.Index))
                    {
                        await SetDeltaIndex(delta);
                    }

                    var record = os.CreateObject<XpoDeltaRecord>();
                    record.DeltaId = delta.DeltaId;
                    record.Date = delta.Date;
                    record.Epoch = delta.Epoch;
                    record.Identity = delta.Identity;
                    record.Index = delta.Index;
                    record.Operation = delta.Operation;
                }

                var savedArgs = new SavedDeltaEventArgs(delta, savingArgs.CustomHandled);
                OnSavedDelta(savedArgs);
            }

            os.CommitChanges();
        }

        public override async Task SetLastProcessedDeltaAsync(string index, string identity, CancellationToken cancellationToken = default)
        {
            IObjectSpace objectSpace = Provider.CreateObjectSpace();
            var state = GetOrCreateDeltaState(identity, objectSpace);
            state.LastProcessedDelta = index;
            objectSpace.CommitChanges();
        }

        public override async Task SetLastPushedDeltaAsync(string index, string identity, CancellationToken cancellationToken = default)
        {
            IObjectSpace objectSpace = Provider.CreateObjectSpace();
            var state = GetOrCreateDeltaState(identity, objectSpace);
            state.LastPushedDelta = index;
            objectSpace.CommitChanges();
        }

        private XpoDeltaState GetOrCreateDeltaState(string identity, IObjectSpace objectSpace)
        {
            var state = objectSpace.FindObject<XpoDeltaState>(
                CriteriaOperator.Parse("Identity = ?", identity));

            if (state == null)
            {
                state = objectSpace.CreateObject<XpoDeltaState>();
                state.Identity = identity;
                state.LastProcessedDelta = SequenceService.GetFirstIndexValue().Result;
                state.LastPushedDelta = SequenceService.GetFirstIndexValue().Result;
            }

            return state;
        }
    }
}