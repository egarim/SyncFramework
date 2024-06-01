using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Update;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.EfCore
{
    public class EfDeltaProcessor : DeltaProcessorBase
    {
        string CurrentDbEngine;

        string connectionString;
        DbContext _dBContext;
        DbProviderFactory factory;
        public EfDeltaProcessor(DbContext dBContext,ISequenceService sequenceService):base(sequenceService)
        {
            _dBContext = dBContext;
        }
        public EfDeltaProcessor(string connectionstring, string DbEngineAlias, string ProviderInvariantName, ISequenceService sequenceService) : base(sequenceService)
        {

            this.CurrentDbEngine = DbEngineAlias;
            this.connectionString = connectionstring;

            try
            {
                factory = DbProviderFactories.GetFactory(ProviderInvariantName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw new Exception("There was a problem creating the database connection using DbProviderFactories.GetFactory. Please your make sure the DbProviderFactory for your database is registered https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbproviderfactories.registerfactory?view=net-5.0", ex);
            }
            //TODO check provider registration later

            //DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);
        }
        protected virtual IDbCommand CreateDbCommand(string CommandDbEngine, IDelta delta, IDbConnection dbConnection, ModificationCommandData modificationCommandData)
        {
            IDbCommand dbCommand = dbConnection.CreateCommand();
          
            var CurrentCommand = modificationCommandData.SqlCommandTexts.FirstOrDefault(c => c.DbEngine == CommandDbEngine);
            if (CurrentCommand == null)
            {
                throw new Exception($"the delta({delta.Index}-{delta.Identity}-{delta.Epoch}) does not contain information for current database  using the updater type:{CommandDbEngine}");
            }
            dbCommand.CommandText = CurrentCommand.Command;
            SetParameters(modificationCommandData, dbCommand);

            return dbCommand;
        }

        protected virtual void SetParameters(ModificationCommandData modificationCommandData, IDbCommand dbCommand)
        {
            foreach (Parameters ModificationCommandParameter in modificationCommandData.parameters)
            {

                IDbDataParameter DbCommandParameter = dbCommand.CreateParameter();

                DbCommandParameter.ParameterName = ModificationCommandParameter.Name;
                if (ModificationCommandParameter.Value == null)
                {

                    DbCommandParameter.Value = DBNull.Value;
                }
                else
                {
                    Debug.WriteLine("Before casting");
                    Debug.WriteLine($"ParameterName:{DbCommandParameter.ParameterName}--Value:{ModificationCommandParameter.Value}--GetType:{ModificationCommandParameter.Value.GetType()}--DbType:{ModificationCommandParameter.DbType}");
                    if ((ModificationCommandParameter.DbType == DbType.Guid) || (ModificationCommandParameter.CrlType == "System.Guid"))
                    {
                        DbCommandParameter.Value = Guid.Parse(ModificationCommandParameter.Value.ToString());
                    }
                    else
                    {
                        DbCommandParameter.Value = ModificationCommandParameter.Value;
                    }
                    Debug.WriteLine("After casting");
                    Debug.WriteLine($"ParameterName:{DbCommandParameter.ParameterName}--Value:{DbCommandParameter.Value}--GetType:{DbCommandParameter.Value.GetType()}--DbType:{DbCommandParameter.DbType}");

                }

                dbCommand.Parameters.Add(DbCommandParameter);
            }
        }

        protected virtual IDbConnection GetConnection(string ConnectionString)
        {
            //HACK https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory

            // Assume failure.
            DbConnection connection = null;

            // Create the DbProviderFactory and DbConnection.
            if (ConnectionString != null)
            {
                try
                {

                    connection = factory.CreateConnection();
                    connection.ConnectionString = ConnectionString;
                }
                catch (Exception ex)
                {
                    // Set the connection to null if it was created.
                    if (connection != null)
                    {
                        connection = null;
                    }
                    Debug.WriteLine(ex.Message);
                    throw new Exception("There was a problem creating the database connection using DbProviderFactories.GetFactory. Please your make sure the DbProviderFactory for your database is registered https://docs.microsoft.com/en-us/dotnet/api/system.data.common.dbproviderfactories.registerfactory?view=net-5.0", ex);
                }
            }
            // Return the connection.
            return connection;
        }
        public override async Task ProcessDeltasAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken)
        {
            
            IDbConnection dbConnection;
            if (this._dBContext == null)
            {
                dbConnection = this.GetConnection(this.connectionString);
            }
            else
            {
                dbConnection = this._dBContext.Database.GetDbConnection();
                var ServiceProvider = this._dBContext as IInfrastructure<IServiceProvider>;
                var IUpdateSqlGenerator = ServiceProvider.GetService<IUpdateSqlGenerator>();
                var UpdaterAliasService = ServiceProvider.GetService<IUpdaterAliasService>() as IUpdaterAliasService;
                this.CurrentDbEngine = UpdaterAliasService.GetAlias(IUpdateSqlGenerator.GetType().FullName);
            }
            Debug.WriteLine($"CurrentDbEngine:{CurrentDbEngine}");
            foreach (IDelta delta in Deltas)
            {

                var processingDeltaEventArgs = new ProcessingDeltaEventArgs(delta);

                // Raise the event
                OnProcessingDelta(processingDeltaEventArgs);

                // Check if the event handling should be canceled
                if (!processingDeltaEventArgs.Handled)
                {
                    List<ModificationCommandData> ModificationsData = this.GetDeltaOperations<List<ModificationCommandData>>(delta);
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        WriteModificationsDataToDebugConsole(ModificationsData);
                    }
                    foreach (ModificationCommandData modificationCommandData in ModificationsData)
                    {

                        IDbCommand dbCommand = CreateDbCommand(CurrentDbEngine, delta, dbConnection, modificationCommandData);

                        if (dbConnection.State != ConnectionState.Open)
                        {
                            dbConnection.Open();
                        }

                        Debug.WriteLine($"Command:{dbCommand.CommandText}--{delta.Identity}");
                        var dbCommandAsync = dbCommand as DbCommand;
                        if (dbCommandAsync != null)
                            await dbCommandAsync.ExecuteNonQueryAsync().ConfigureAwait(false);
                        else
                            dbCommand.ExecuteNonQuery();


                        ProcessDeltaBaseEventArgs saveDeltaBaseEventArgs = new ProcessDeltaBaseEventArgs(delta);
                        OnProcessedDelta(saveDeltaBaseEventArgs);
                    }
                }
               
            }


        }

        private static void WriteModificationsDataToDebugConsole(List<ModificationCommandData> ModificationsData)
        {
            foreach (ModificationCommandData modificationCommandData in ModificationsData)
            {
                foreach (EfSqlCommandData sqlCommandText in modificationCommandData.SqlCommandTexts)
                {
                    Debug.WriteLine($"{sqlCommandText.DbEngine}-{sqlCommandText.Command}");
                }
                foreach (Parameters parameters in modificationCommandData.parameters)
                {
                    Debug.WriteLine($"{parameters.Name} : {parameters.Value}");
                }

            }
        }
    }
}