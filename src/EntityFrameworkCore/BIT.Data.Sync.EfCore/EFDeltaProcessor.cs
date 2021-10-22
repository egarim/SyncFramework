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
    public class EFDeltaProcessor : DeltaProcessorBase
    {
        string CurrentDbEngine;

        string connectionString;
        DbContext _dBContext;
        DbProviderFactory factory;
        public EFDeltaProcessor(DbContext dBContext) 
        {
            _dBContext = dBContext;
          
        }
        public EFDeltaProcessor(string connectionstring, string DbEngineAlias, string ProviderInvariantName)
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
        protected virtual IDbCommand CreateDbCommand(string CommandDbEngine, IDelta delta, IDbConnection dbConnection, ModificationCommandData modificationCommandData, List<object> Parameters, List<IDbDataParameter> SqlParameters)
        {
            IDbCommand dbCommand = dbConnection.CreateCommand();
            //TODO pass the command that is type of the target Database
            var CurrentCommand = modificationCommandData.SqlCommandTexts.FirstOrDefault(c => c.DbEngine == CommandDbEngine);
            if (CurrentCommand == null)
            {
                throw new Exception($"the delta({delta.Index}-{delta.Identity}-{delta.Epoch}) does not contain information for current database  using the updater type:{CommandDbEngine}");
            }
            dbCommand.CommandText = CurrentCommand.Command;
            foreach (Parameters parameters in modificationCommandData.parameters)
            {

                var DbCommandParameter = dbCommand.CreateParameter();
                DbCommandParameter.ParameterName = parameters.Name;
                if (parameters.Value == null)
                {

                    DbCommandParameter.Value = DBNull.Value;
                }
                else
                {
                    DbCommandParameter.Value = parameters.Value;
                }


                SqlParameters.Add(DbCommandParameter);
                Parameters.Add(parameters.Value);

                dbCommand.Parameters.Add(DbCommandParameter);
            }

            return dbCommand;
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

            foreach (IDelta delta in Deltas)
            {
               
               

                List<ModificationCommandData> ModificationsData = this.GetDeltaOperations<List<ModificationCommandData>>(delta);
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    WriteModificationsDataToDebugConsole(ModificationsData);
                }


                foreach (ModificationCommandData modificationCommandData in ModificationsData)
                {
                    List<object> Parameters = new List<object>();
                    List<IDbDataParameter> SqlParameters = new List<IDbDataParameter>();
                    IDbCommand dbCommand = CreateDbCommand(CurrentDbEngine, delta, dbConnection, modificationCommandData, Parameters, SqlParameters);
                    dbConnection.Open();
                    var dbCommandAsync = dbCommand as DbCommand;
                    if (dbCommandAsync != null)
                        await dbCommandAsync.ExecuteNonQueryAsync().ConfigureAwait(false);
                    else
                        dbCommand.ExecuteNonQuery();
                    dbConnection.Close();

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