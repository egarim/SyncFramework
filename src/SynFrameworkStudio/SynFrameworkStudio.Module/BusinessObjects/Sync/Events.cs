using DevExpress.Xpo;
using BIT.Data.Sync;
using System;
using System.Collections.Generic;
using System.Text.Json;
using DevExpress.Persistent.BaseImpl;
using BIT.Data.Sync.Client;
using DevExpress.Persistent.Base;
using DevExpress.XtraPrinting.BrickExporters;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    public enum EventType
    {
        Fetch=0,
        Push=1
    }
    
    [Persistent("PushOperationResponses")]
    [DefaultClassOptions()]
    public class Events : BaseObject
    {
        public Events(Session session) : base(session) { }

        string clientNodeId;
        EventType eventType;
        TimeOnly time;
        DateOnly date;

        ServerNode serverNode;
        string _errorMessage;
        [Size(SizeAttribute.Unlimited)]
        public string Message
        {
            get => _errorMessage;
            set => SetPropertyValue(nameof(Message), ref _errorMessage, value);
        }


        public EventType EventType
        {
            get => eventType;
            set => SetPropertyValue(nameof(EventType), ref eventType, value);
        }
        public DateOnly Date
        {
            get => date;
            set => SetPropertyValue(nameof(Date), ref date, value);
        }

        public TimeOnly Time
        {
            get => time;
            set => SetPropertyValue(nameof(Time), ref time, value);
        }

        bool _success;
        public bool Success
        {
            get => _success;
            set => SetPropertyValue(nameof(Success), ref _success, value);
        }
        
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string ClientNodeId
        {
            get => clientNodeId;
            set => SetPropertyValue(nameof(ClientNodeId), ref clientNodeId, value);
        }
        [Association("ServerNode-PersistentPushOperationResponses")]
        public ServerNode ServerNode
        {
            get => serverNode;
            set => SetPropertyValue(nameof(ServerNode), ref serverNode, value);
        }
        string _processedDeltaIdsJson;
        [Size(SizeAttribute.Unlimited)]
        public string ProcessedDeltaIdsJson
        {
            get => _processedDeltaIdsJson;
            set => SetPropertyValue(nameof(ProcessedDeltaIdsJson), ref _processedDeltaIdsJson, value);
        }

        public void LoadFrom(FetchOperationResponse response)
        {
            Success = response.Success;
            Message = response.Message;
            EventType = EventType.Fetch;
            this.ServerNode = Session.Query<ServerNode>().FirstOrDefault(x => x.NodeId == response.ServerNodeId);
            ClientNodeId = response.ClientNodeId;
        }
        public void LoadFrom(PushOperationResponse response)
        {
            Success = response.Success;
            Message = response.Message;
            EventType = EventType.Push;
            this.ServerNode = Session.Query<ServerNode>().FirstOrDefault(x => x.NodeId == response.ServerNodeId);
            ProcessedDeltaIdsJson = JsonSerializer.Serialize(response.ProcessedDeltasIds);
            ClientNodeId= response.ClientNodeId;
        }

        public PushOperationResponse ToPushOperationResponse()
        {
            return new PushOperationResponse
            {
                Success = this.Success,
                Message = this.Message,
                ProcessedDeltasIds = JsonSerializer.Deserialize<List<string>>(ProcessedDeltaIdsJson)
            };
        }
    }
}