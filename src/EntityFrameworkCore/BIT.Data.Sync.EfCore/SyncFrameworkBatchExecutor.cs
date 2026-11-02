using BIT.Data.Sync.EfCore.Data;
using BIT.EfCore.Sync;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using BIT.Data.Sync.Client;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;



namespace BIT.Data.Sync.EfCore
{
    public class SyncFrameworkBatchExecutor : BatchExecutor
    {



        public IDeltaStore DeltaStore { get; set; }
        ISyncIdentityService syncIdentityService { get; set; }

        public SyncFrameworkBatchExecutor([NotNull] ICurrentDbContext currentContext, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger) : base(currentContext, updateLogger)
        {

        }
        public SyncFrameworkBatchExecutor([NotNull] ICurrentDbContext currentContext, [NotNull] IDiagnosticsLogger<DbLoggerCategory.Update> updateLogger, IDeltaStore DeltaStore, ISyncIdentityService syncIdentityService) : base(currentContext, updateLogger)
        {

            this.DeltaStore = DeltaStore;
            this.syncIdentityService = syncIdentityService;
        }
        public override int Execute(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection)
        {

          
            int AffectedRows = base.Execute(commandBatches, connection);
            // Run on the thread pool so blocking here can't deadlock a caller with a SynchronizationContext (UI threads).
            if (IsDeltaTrackingEnabled)
                Task.Run(() => SaveDeltasAsync(commandBatches, AffectedRows)).GetAwaiter().GetResult();

            return AffectedRows;
        }

        /// <summary>
        /// False when the current context implements <see cref="IEnableDeltaTracking"/> and has tracking turned off.
        /// </summary>
        protected virtual bool IsDeltaTrackingEnabled => (this.CurrentContext.Context as IEnableDeltaTracking)?.EnableDeltaTracking ?? true;
        public async override Task<int> ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
          
            int AffectedRows = await base.ExecuteAsync(commandBatches, connection, cancellationToken).ConfigureAwait(false);
            if (IsDeltaTrackingEnabled)
                await SaveDeltasAsync(commandBatches, AffectedRows, cancellationToken).ConfigureAwait(false);
            return AffectedRows;
        }

        private async Task<List<ModificationCommandData>> SaveDeltasAsync(IEnumerable<ModificationCommandBatch> commandBatches,int AffectedRows, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var EFSyncFrameworkService = this.CurrentContext.Context.GetService<IModificationCommandToCommandDataService>();
            var CurrentUpdater = this.CurrentContext.Context.GetService<IUpdateSqlGenerator>();
            var IUpdaterAliasService = this.CurrentContext.Context.GetService<IUpdaterAliasService>();

            var alias=IUpdaterAliasService.GetAlias(CurrentUpdater.GetType().FullName);


            List<ModificationCommandData> modifications = new List<ModificationCommandData>();
            IReadOnlyList<DeltaRoute> routes = (this.CurrentContext.Context as IRoutedClientSyncDatabase)?.Routes ?? new List<DeltaRoute>();
            Dictionary<DeltaRoute, List<ModificationCommandData>> routedModifications = new Dictionary<DeltaRoute, List<ModificationCommandData>>();
            foreach (ModificationCommandBatch modificationCommandBatch in commandBatches)
            {

                foreach (ModificationCommand modificationCommandItem in modificationCommandBatch.ModificationCommands)
                {
                    ModificationCommand modificationCommand = modificationCommandItem;
                    IEnumerable<EfSqlCommandData> commands = null;

                    switch (modificationCommand.EntityState)
                    {
                        case EntityState.Detached:
                            break;
                        case EntityState.Unchanged:
                            break;
                        case EntityState.Deleted:
                            commands = EFSyncFrameworkService.AppendDeleteOperation(modificationCommand);
                            break;
                        case EntityState.Modified:
                            commands = EFSyncFrameworkService.AppendUpdateOperation(modificationCommand);
                            break;
                        case EntityState.Added:
                            commands = EFSyncFrameworkService.AppendInsertOperation(modificationCommand);
                            break;
                    }
                    List<Parameters> parameters = GetParameters(modificationCommand);

                    ModificationCommandData modificationCommandData = new ModificationCommandData(parameters, commands, AffectedRows, alias);
                    DeltaRoute route = routes.FirstOrDefault(r => r.Handles(modificationCommand.TableName, modificationCommand.Schema));
                    if (route == null)
                    {
                        modifications.Add(modificationCommandData);
                    }
                    else
                    {
                        if (!routedModifications.TryGetValue(route, out var routeModifications))
                            routedModifications[route] = routeModifications = new List<ModificationCommandData>();
                        routeModifications.Add(modificationCommandData);
                    }
                }
            }

            // Without routes this is the original behaviour: one delta per SaveChanges on the default store.
            if (modifications.Count > 0 || routes.Count == 0)
            {
                var Delta = DeltaStore.CreateDelta(syncIdentityService.Identity, modifications);
                await this.DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, cancellationToken).ConfigureAwait(false);
            }
            foreach (var routed in routedModifications)
            {
                var Delta = routed.Key.DeltaStore.CreateDelta(syncIdentityService.Identity, routed.Value);
                await routed.Key.DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, cancellationToken).ConfigureAwait(false);
            }


            return modifications;
        }

        protected virtual List<Parameters> GetParameters(ModificationCommand modificationCommand)
        {
            List<Parameters> parameters = new List<Parameters>();
            foreach (ColumnModification columnModificationItem in modificationCommand.ColumnModifications)
            {
                ColumnModification columnModification = columnModificationItem;

                Parameters item = new Parameters()
                {
                    /*columnModification.OriginalParameterName should be used in case of columnModification.ParameterName is null. Due to this it will throw unknown parametter error.*/
                    Name = !string.IsNullOrEmpty(columnModification.ParameterName)? columnModification.ParameterName: columnModification.OriginalParameterName,
                    DbType = columnModification.TypeMapping.DbType,
                    CrlType = columnModification.TypeMapping.ClrType.FullName
                };
                if (columnModification.UseCurrentValueParameter)
                {
                    item.Value = columnModification.Value;
                }
                if (columnModification.UseOriginalValueParameter)
                {
                    item.Value = columnModification.OriginalValue;
                }
                parameters.Add(item);
            }

            return parameters;
        }
    }
  
}
