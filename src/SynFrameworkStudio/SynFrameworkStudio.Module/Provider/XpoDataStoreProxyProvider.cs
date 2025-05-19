using System;
using DevExpress.Xpo.Metadata;
using DevExpress.ExpressApp.Xpo;

namespace SynFrameworkStudio.Module.Provider
{
    public interface IConnectToNodeData
    {
        void Connect(string ConnectionString);
    }
    public class XpoDataStoreProxyProvider : IXpoDataStoreProvider, IConnectToNodeData
    {
		private XpoDataStoreProxy proxy;
		public XpoDataStoreProxyProvider() {
			proxy = new XpoDataStoreProxy();
		}
		public DevExpress.Xpo.DB.IDataStore CreateUpdatingStore(bool allowUpdateSchema, out IDisposable[] disposableObjects) {
			disposableObjects = null;
			return proxy;
		}
		public DevExpress.Xpo.DB.IDataStore CreateWorkingStore(out IDisposable[] disposableObjects) {
			disposableObjects = null;
			return proxy;
		}
		public DevExpress.Xpo.DB.IDataStore CreateSchemaCheckingStore(out IDisposable[] disposableObjects) {
			disposableObjects = null;
			return proxy;
		}
		public XPDictionary XPDictionary {
			get { return null; }
		}
		public string ConnectionString {
			get { return null; }
		}
		public bool IsInitialized {
			get;
			private set;
		}
		public void Initialize(XPDictionary dictionary, string legacyConnectionString, string tempConnectionString) {
			proxy.Initialize(dictionary, legacyConnectionString, tempConnectionString);
			IsInitialized = true;
		}

        public void Connect(string ConnectionString)
        {
            proxy.CreateOrUpdateSyncDataLayer(ConnectionString);
        }
    }
}
