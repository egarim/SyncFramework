using System.Linq;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.Helpers;
using DevExpress.Xpo.Metadata;
using System.Collections.Generic;

namespace SynFrameworkStudio.Module.Provider
{
    public class XpoDataStoreProxy : IDataStore, ICommandChannel {
        private SimpleDataLayer appDataLayer;
        private IDataStore appDataStore;
        private SimpleDataLayer syncDataLayer;
        private IDataStore syncDataStore;
        private string[] syncDatabaseTables = new string[] { "XpoDeltaRecord", "XpoDeltaState", "XpoSequence" };
        ReflectionDictionary syncDictionary;

        private bool IsSyncTable(string tableName) {
            if(!string.IsNullOrEmpty(tableName)) {
                foreach(string currentTableName in syncDatabaseTables) {
                    if(tableName.EndsWith(currentTableName)) {
                        return true;
                    }
                }
            }
            return false;
        }
        public void Initialize(XPDictionary dictionary, string legacyConnectionString, string tempConnectionString)
        {
            ReflectionDictionary appDictionary = new ReflectionDictionary();
            syncDictionary = new ReflectionDictionary();
            foreach (XPClassInfo ci in dictionary.Classes)
            {
                if (!IsSyncTable(ci.TableName))
                {
                    appDictionary.QueryClassInfo(ci.ClassType);
                }
                else
                {
                    syncDictionary.QueryClassInfo(ci.ClassType);
                }
            }
            appDataStore = XpoDefault.GetConnectionProvider(legacyConnectionString, AutoCreateOption.DatabaseAndSchema);
            appDataLayer = new SimpleDataLayer(appDictionary, appDataStore);

            CreateOrUpdateSyncDataLayer(tempConnectionString);
        }

        public void CreateOrUpdateSyncDataLayer(string tempConnectionString)
        {
            syncDataStore = XpoDefault.GetConnectionProvider(tempConnectionString, AutoCreateOption.DatabaseAndSchema);
            syncDataLayer = new SimpleDataLayer(syncDictionary, syncDataStore);
        }

        public AutoCreateOption AutoCreateOption {
            get {
                return AutoCreateOption.DatabaseAndSchema;
            }
        }
        public ModificationResult ModifyData(params ModificationStatement[] dmlStatements) {
            List<ModificationStatement> legacyChanges = new List<ModificationStatement>(dmlStatements.Length);
            List<ModificationStatement> tempChanges = new List<ModificationStatement>(dmlStatements.Length);
            foreach(ModificationStatement stm in dmlStatements) {
                if(IsSyncTable(stm.Table.Name)) {
                    tempChanges.Add(stm);
                }
                else {
                    legacyChanges.Add(stm);
                }
            }

            List<ParameterValue> resultSet = new List<ParameterValue>();
            if(legacyChanges.Count > 0) {
                resultSet.AddRange(appDataLayer.ModifyData(legacyChanges.ToArray()).Identities);
            }
            if(tempChanges.Count > 0) {
                resultSet.AddRange(syncDataLayer.ModifyData(tempChanges.ToArray()).Identities);
            }
            return new ModificationResult(resultSet);
        }
        public SelectedData SelectData(params SelectStatement[] selects) {
            var isExternals = selects.Select(stmt => IsSyncTable(stmt.Table.Name)).ToList();
            List<SelectStatement> mainSelects = new List<SelectStatement>(selects.Length);
            List<SelectStatement> externalSelects = new List<SelectStatement>(selects.Length);
            for(int i = 0; i < isExternals.Count; ++i) {
                (isExternals[i] ? externalSelects : mainSelects).Add(selects[i]);
            }
            var externalResults = (externalSelects.Count == 0 ? Enumerable.Empty<SelectStatementResult>() : syncDataLayer.SelectData(externalSelects.ToArray()).ResultSet).GetEnumerator();
            var mainResults = (mainSelects.Count == 0 ? Enumerable.Empty<SelectStatementResult>() : appDataLayer.SelectData(mainSelects.ToArray()).ResultSet).GetEnumerator();
            SelectStatementResult[] results = new SelectStatementResult[isExternals.Count];
            for(int i = 0; i < results.Length; ++i) {
                var enumerator = isExternals[i] ? externalResults : mainResults;
                enumerator.MoveNext();
                results[i] = enumerator.Current;
            }
            return new SelectedData(results);
        }
        public UpdateSchemaResult UpdateSchema(bool dontCreateIfFirstTableNotExist, params DBTable[] tables) {
            List<DBTable> db1Tables = new List<DBTable>();
            List<DBTable> db2Tables = new List<DBTable>();

            foreach(DBTable table in tables) {
                if(!IsSyncTable(table.Name)) {
                    db1Tables.Add(table);
                }
                else {
                    db2Tables.Add(table);
                }
            }
            appDataStore.UpdateSchema(false, db1Tables.ToArray());
            syncDataStore.UpdateSchema(false, db2Tables.ToArray());
            return UpdateSchemaResult.SchemaExists;
        }
        public object Do(string command, object args) {
            return ((ICommandChannel)appDataLayer).Do(command, args);
        }
    }
}