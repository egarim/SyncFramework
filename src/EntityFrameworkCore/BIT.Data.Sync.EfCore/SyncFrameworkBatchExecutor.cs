using BIT.Data.Sync.EfCore.Data;
using BIT.EfCore.Sync;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Update.Internal;
using System.Collections.Generic;
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
            SaveDeltasAsync(commandBatches, AffectedRows).ConfigureAwait(false);
           
            return AffectedRows;
        }
        public async override Task<int> ExecuteAsync(IEnumerable<ModificationCommandBatch> commandBatches, IRelationalConnection connection, CancellationToken cancellationToken = default)
        {
            var test = connection.DbConnection.GetType();
            cancellationToken.ThrowIfCancellationRequested();
          
            int AffectedRows = await base.ExecuteAsync(commandBatches, connection, cancellationToken).ConfigureAwait(false);
            await SaveDeltasAsync(commandBatches, AffectedRows, cancellationToken).ConfigureAwait(false);
            return AffectedRows;
        }

        private async Task<List<ModificationCommandData>> SaveDeltasAsync(IEnumerable<ModificationCommandBatch> commandBatches,int AffectedRows, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var EFSyncFrameworkService = this.CurrentContext.Context.GetService<IModificationCommandToCommandData>();
            //var CurrentUpdater = this.CurrentContext.Context.GetService<IUpdateSqlGenerator>();

            //HACK to get the command in a different provider you need to execute the same code as in ReaderModificationCommandBatch.CreateStoreCommand
            //StringBuilder commandStringBuilder = new StringBuilder();
            //StringBuilder originalSqlBuilder = new StringBuilder();
            List<ModificationCommandData> modifications = new List<ModificationCommandData>();
            foreach (ModificationCommandBatch modificationCommandBatch in commandBatches)
            {

                StringBuilder originalSqlBuilder = new StringBuilder();
                for (int i = 0; i < modificationCommandBatch.ModificationCommands.Count; i++)
                {
                    ModificationCommand modificationCommand = modificationCommandBatch.ModificationCommands[i];
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
                    List<Parameters> parameters = new List<Parameters>();
                    for (int j = 0; j < modificationCommand.ColumnModifications.Count; j++)
                    {
                        ColumnModification columnModification = modificationCommand.ColumnModifications[j];
                        if (columnModification.UseCurrentValueParameter)
                        {

                            parameters.Add(new Parameters() { Name = columnModification.ParameterName, Value = columnModification.Value });
                        }

                        if (columnModification.UseOriginalValueParameter)
                        {
                            parameters.Add(new Parameters() { Name = columnModification.OriginalParameterName, Value = columnModification.OriginalValue });
                        }
                    }

                    ModificationCommandData modificationCommandData = new ModificationCommandData(parameters, commands, AffectedRows);
                    modifications.Add(modificationCommandData);
                }
            }

            var Delta= DeltaStore.CreateDelta(syncIdentityService.Identity, modifications);

            await this.DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, cancellationToken).ConfigureAwait(false);


            return modifications;
        }
    }
  
}
