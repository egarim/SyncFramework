using DevExpress.Xpo;
using System.Security.Principal;

namespace BIT.Data.Sync.Xpo.DeltaStore
{
    public class XpoDeltaStore: DeltaStoreBase
    {
        public IDataLayer dataLayer { get; set; }
        protected XpoDeltaStore(ISequenceService sequenceService):base(sequenceService)
        {

        }
        public XpoDeltaStore(IDataLayer dataLayer, ISequenceService sequenceService, bool initSchema = false) : base(sequenceService)
        {
            this.dataLayer = dataLayer;
          
            if (initSchema)
            {
                this.InitSchema();
            }

        }
        void InitSchema()
        {
            IDataLayer dl = dataLayer;
            using (Session session = new Session(dl))
            {
                System.Reflection.Assembly[] assemblies = new System.Reflection.Assembly[] {
                typeof(XpoDelta).Assembly};
                session.UpdateSchema(assemblies);
                session.CreateObjectTypeRecords(assemblies);
            }
        }

        protected virtual UnitOfWork GetUnitOfWork()
        {
            return new UnitOfWork(dataLayer);
        }
        public override async Task<bool> CanRestoreDatabaseAsync(string identity, CancellationToken cancellationToken)
        {
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            return  await UoW.Query<XpoSyncStatus>().AnyAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<int> GetDeltaCountAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            return await UoW.Query<XpoDelta>().CountAsync(d => d.Index.CompareTo(startIndex) > 0 && d.Identity == identity);
        }

        public override Task<IEnumerable<IDelta>> GetDeltasAsync(string startIndex, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            startIndex = GuardStartIndex(startIndex);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(UoW.Query<XpoDelta>().Where(d => d.Index.CompareTo(startIndex) > 0).ToList().Cast<IDelta>()); 
        }

        public override Task<IEnumerable<IDelta>> GetDeltasByIdentityAsync(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            startIndex = GuardStartIndex(startIndex);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(UoW.Query<XpoDelta>().Where(d => d.Index.CompareTo(startIndex) > 0).ToList().Cast<IDelta>());
        }

        public override async Task<IEnumerable<IDelta>> GetDeltasFromOtherNodes(string startIndex, string identity, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var UoW = GetUnitOfWork();
            IQueryable<XpoDelta> result = UoW.Query<XpoDelta>().Where(d => d.Index.CompareTo(startIndex) > 0 && d.Identity != identity);
           var Deltas = result.ToList();
            return await Task.FromResult(Deltas.Cast<IDelta>()).ConfigureAwait(false);
        }

        public override async Task<string> GetLastProcessedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await UoW.Query<XpoSyncStatus>().FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return string.Empty;
            else
                return CurrentDeltaIndex.LastProcessedDelta;
        }

        public override async Task<string> GetLastPushedDeltaAsync(string identity, CancellationToken cancellationToken = default)
        { 
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            var CurrentDeltaIndex = await UoW.Query<XpoSyncStatus>().FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
                return string.Empty;
            else
                return CurrentDeltaIndex.LastPushedDelta;
        }

        public override async Task PurgeDeltasAsync(string identity, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            var deltas = UoW.Query<XpoDelta>().Where(d => d.Identity == identity).ToList();
            UoW.Delete(deltas);
            await UoW.CommitChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task ResetDeltasStatusAsync(string identity, CancellationToken cancellationToken = default)
        {
            var UoW = GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            var status = await UoW.Query<XpoSyncStatus>().FirstOrDefaultAsync(d => d.Identity == identity).ConfigureAwait(false);
            if (status != null)
            {
                UoW.Delete(status);
                await UoW.CommitChangesAsync(cancellationToken);
            }
        }

        public override async Task SaveDeltasAsync(IEnumerable<IDelta> deltas, CancellationToken cancellationToken = default)
        {
            var UoW=GetUnitOfWork();
            cancellationToken.ThrowIfCancellationRequested();
            foreach (IDelta delta in deltas)
            {
                await SetDeltaIndex(delta);
                XpoDelta entity = new XpoDelta(UoW);
                entity.Identity = delta.Identity;
                entity.Index = delta.Index;
                entity.Operation = delta.Operation;
                entity.Epoch = delta.Epoch;
                entity.Date = delta.Date;
                entity.Index = delta.Index;



            }
            await UoW.CommitChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override async Task SetLastProcessedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
            var UoW = GetUnitOfWork();
            var CurrentDeltaIndex = await UoW.Query<XpoSyncStatus>().FirstOrDefaultAsync(f => f.Identity == identity, cancellationToken).ConfigureAwait(false);
            if (CurrentDeltaIndex == null)
            {
                CurrentDeltaIndex = new XpoSyncStatus(UoW)
                {
                    Identity = identity
                };
              
            }

            CurrentDeltaIndex.LastProcessedDelta = Index;

            await UoW.CommitChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public override Task SetLastPushedDeltaAsync(string Index, string identity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}