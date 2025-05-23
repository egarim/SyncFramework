using BIT.Data.Sync.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Imp
{
    public class SimpleDatabase : IClientSyncDatabase, IEnableDeltaTracking
    {
        public IDeltaProcessor DeltaProcessor { get; protected set; }
        public string Identity { get; set; }
        public IDeltaStore DeltaStore { get; protected set; }
        public List<SimpleDatabaseRecord> Data { get => _Data; protected set => _Data = value; }
        public ISyncFrameworkClient SyncFrameworkClient  { get; protected set ;}
        public bool EnableDeltaTracking { get; set; }

        List<SimpleDatabaseRecord> _Data;
        public SimpleDatabase(IDeltaStore deltaStore, string identity,  List<SimpleDatabaseRecord> Data, ISyncFrameworkClient syncFrameworkClient,bool EnableDeltaTracking= true)
        {
            Identity = identity;
            DeltaStore = deltaStore;
            this.Data= Data;
            this.SyncFrameworkClient = syncFrameworkClient;
            this.DeltaProcessor = new SimpleDatabaseDeltaProcessor(this.Data, deltaStore.SequenceService);
            this.EnableDeltaTracking = EnableDeltaTracking;
            
        
        }
        public SimpleDatabase(IDeltaStore deltaStore, string identity, ISyncFrameworkClient SyncFrameworkClient,bool EnableDeltaTracking=true) :this(deltaStore, identity,new List<SimpleDatabaseRecord>(), SyncFrameworkClient, EnableDeltaTracking)
        {
         
        }
        public SimpleDatabase(string identity, ISyncFrameworkClient SyncFrameworkClient) : this(new MemoryDeltaStore(), identity, new List<SimpleDatabaseRecord>(), SyncFrameworkClient)
        {

        }
        public SimpleDatabase(IDeltaStore deltaStore, string identity) : this(deltaStore, identity, new List<SimpleDatabaseRecord>(), null)
        {

        }
        public async void Update(SimpleDatabaseRecord Instance)
        {
            var ObjectToUpdate = Data.FirstOrDefault(x => x.Key == Instance.Key);
            if (ObjectToUpdate != null)
            {
                var Index = Data.IndexOf(ObjectToUpdate);
                Data[Index] = Instance;
                if(EnableDeltaTracking)
                {
                    SimpleDatabaseModification item = new SimpleDatabaseModification(OperationType.Update, Instance);
                    await SaveDelta(item);
                }
               
            }
          
        }

        private async Task SaveDelta(SimpleDatabaseModification item)
        {
            var Delta = DeltaStore.CreateDelta(Identity,item);
           
            if (EnableDeltaTracking)
            {
                await DeltaStore.SaveDeltasAsync(new List<IDelta>() { Delta }, default);
            }
        }

        public async void Delete(SimpleDatabaseRecord Instance)
        {
            var ObjectToDelete=  Data.FirstOrDefault(x=>x.Key==Instance.Key);
            if(ObjectToDelete!=null)
            {
                Data.Remove(ObjectToDelete);
                if (EnableDeltaTracking)
                {
                    SimpleDatabaseModification item = new SimpleDatabaseModification(OperationType.Delete, Instance);
                    await SaveDelta(item);
                }
               
            }
           
        }
        public async Task Add(SimpleDatabaseRecord Instance)
        {
            Data.Add(Instance);
            if (EnableDeltaTracking)
            {
                SimpleDatabaseModification item = new SimpleDatabaseModification(OperationType.Add, Instance);
                await SaveDelta(item);
            }
           
        }
    }
}
