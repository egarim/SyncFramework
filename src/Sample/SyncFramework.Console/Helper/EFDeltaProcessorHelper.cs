using BIT.Data.Sync;
using BIT.Data.Sync.EfCore;
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncFramework.ConsoleApp.Helper
{
  
    public class EFDeltaProcessorHelper
    {
        string CurrentDbEngine;

        string connectionString;
        //DbContext _dBContext;
        DbProviderFactory factory;
        //public DeltaProcessorHelper(DbContext dBContext)
        //{
        //    _dBContext = dBContext;

        //}
        public EFDeltaProcessorHelper()
        {

        }
        public virtual IDbCommand CreateDbCommand(string CommandDbEngine, IDelta delta, IDbConnection dbConnection, ModificationCommandData modificationCommandData)
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
