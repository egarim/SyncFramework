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
using System;
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

            if (IsDeltaTrackingEnabled && IsRowChangeCaptureEnabled)
                EnsureRowChangesCanBeCaptured();
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

        /// <summary>
        /// True when the current context implements <see cref="IEnableRowChangeCapture"/> and has row change capture turned on; off by default.
        /// </summary>
        protected virtual bool IsRowChangeCaptureEnabled => (this.CurrentContext.Context as IEnableRowChangeCapture)?.EnableRowChangeCapture ?? false;
        public async override Task<int> ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken = default)
        {
            
            cancellationToken.ThrowIfCancellationRequested();
            if (IsDeltaTrackingEnabled && IsRowChangeCaptureEnabled)
                EnsureRowChangesCanBeCaptured();
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
            bool captureRowChanges = IsRowChangeCaptureEnabled;


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

                    ModificationCommandData modificationCommandData = captureRowChanges
                        ? new ModificationCommandDataWithRowChange(parameters, commands, AffectedRows, alias, GetRowChange(modificationCommand))
                        : new ModificationCommandData(parameters, commands, AffectedRows, alias);
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
                var Delta = DeltaStore.CreateDelta(syncIdentityService.Identity, ToPayload(modifications, captureRowChanges));
                await this.DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, cancellationToken).ConfigureAwait(false);
            }
            foreach (var routed in routedModifications)
            {
                var Delta = routed.Key.DeltaStore.CreateDelta(syncIdentityService.Identity, ToPayload(routed.Value, captureRowChanges));
                await routed.Key.DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, cancellationToken).ConfigureAwait(false);
            }


            return modifications;
        }

        /// <summary>
        /// The object serialized into a delta. Without row change capture it is the list itself, so the payload is unchanged;
        /// with it, a list of <see cref="ModificationCommandDataWithRowChange"/>, so the serializer writes the extra member without type hints.
        /// </summary>
        private static object ToPayload(List<ModificationCommandData> modifications, bool captureRowChanges)
            => captureRowChanges ? modifications.Cast<ModificationCommandDataWithRowChange>().ToList() : (object)modifications;

        /// <summary>
        /// Builds the provider-neutral row change of <paramref name="modificationCommand"/>, or null for a state that changes no row.
        /// Insert: the keys and every written column. Update: the keys (original values) and the written columns.
        /// Delete: the keys only. Values are the provider values, after any value converter.
        /// </summary>
        protected virtual RowChange GetRowChange(ModificationCommand modificationCommand)
        {
            RowChangeOperation operation;
            switch (modificationCommand.EntityState)
            {
                case EntityState.Added: operation = RowChangeOperation.Insert; break;
                case EntityState.Modified: operation = RowChangeOperation.Update; break;
                case EntityState.Deleted: operation = RowChangeOperation.Delete; break;
                default: return null;
            }

            var rowChange = new RowChange
            {
                Table = string.IsNullOrEmpty(modificationCommand.Schema) ? modificationCommand.TableName : modificationCommand.Schema + "." + modificationCommand.TableName,
                Operation = operation
            };
            foreach (IColumnModification column in modificationCommand.ColumnModifications)
            {
                if (column.IsKey)
                {
                    object key = operation == RowChangeOperation.Insert || !column.UseOriginalValueParameter ? column.Value : column.OriginalValue;
                    rowChange.Keys.Add(new RowColumn(column.ColumnName, ToProviderValue(column, key)));
                }
                else if (operation != RowChangeOperation.Delete && column.IsWrite)
                {
                    rowChange.Columns.Add(new RowColumn(column.ColumnName, ToProviderValue(column, column.Value)));
                }
            }
            return rowChange;
        }

        private static object ToProviderValue(IColumnModification column, object value)
        {
            var converter = column.TypeMapping?.Converter;
            if (value == null || converter == null)
                return value;
            var modelType = Nullable.GetUnderlyingType(converter.ModelClrType) ?? converter.ModelClrType;
            return modelType.IsInstanceOfType(value) ? converter.ConvertToProvider(value) : value;
        }

        /// <summary>
        /// Refuses, before anything is written, a save whose changed columns hold a type a row change cannot carry
        /// (see <see cref="RowValueTypes.Allowed"/>), so a failed capture never leaves saved rows without a delta.
        /// </summary>
        protected virtual void EnsureRowChangesCanBeCaptured()
        {
            var changeTracker = this.CurrentContext.Context.ChangeTracker;
            bool autoDetectChanges = changeTracker.AutoDetectChangesEnabled;
            changeTracker.AutoDetectChangesEnabled = false;
            try
            {
                foreach (var entry in changeTracker.Entries())
                {
                    if (entry.State != EntityState.Added && entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
                        continue;
                    foreach (var property in entry.Properties)
                    {
                        // Updates carry the keys and the modified columns, deletes only the keys.
                        bool carried = property.Metadata.IsPrimaryKey()
                            || entry.State == EntityState.Added
                            || (entry.State == EntityState.Modified && property.IsModified);
                        if (!carried)
                            continue;
                        var mapping = property.Metadata.FindTypeMapping();
                        var providerType = mapping?.Converter?.ProviderClrType ?? mapping?.ClrType ?? property.Metadata.ClrType;
                        providerType = Nullable.GetUnderlyingType(providerType) ?? providerType;
                        if (!RowValueTypes.Allowed.Contains(providerType))
                            throw new InvalidOperationException(
                                $"Row change capture is on, but '{entry.Metadata.DisplayName()}.{property.Metadata.Name}' is stored as '{providerType.FullName}', which a row change cannot carry. " +
                                $"Map it to one of {string.Join(", ", RowValueTypes.Allowed.Select(t => t.Name))} with a value converter, or turn row change capture off. Nothing was saved.");
                    }
                }
            }
            finally
            {
                changeTracker.AutoDetectChangesEnabled = autoDetectChanges;
            }
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
