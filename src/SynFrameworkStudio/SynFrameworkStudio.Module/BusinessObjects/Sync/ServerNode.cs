﻿using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl;
using DevExpress.Persistent.Validation;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SynFrameworkStudio.Module.BusinessObjects.Sync
{
    [DefaultClassOptions]
    [NavigationItem("Sync")]
    //[ImageName("BO_Contact")]
    //[DefaultProperty("DisplayMemberNameForLookupEditorsOfThisType")]
    //[DefaultListViewOptions(MasterDetailMode.ListViewOnly, false, NewItemRowPosition.None)]
    //[Persistent("DatabaseTableName")]
    // Specify more UI options using a declarative approach (https://docs.devexpress.com/eXpressAppFramework/112701/business-model-design-orm/data-annotations-in-data-model).
    public class ServerNode : BaseObject
    { // Inherit from a different class to provide a custom primary key, concurrency and deletion behavior, etc. (https://docs.devexpress.com/eXpressAppFramework/113146/business-model-design-orm/business-model-design-with-xpo/base-persistent-classes).
        // Use CodeRush to create XPO classes and properties with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/118557
        public ServerNode(Session session)
            : base(session)
        {
            
        }
        public override void AfterConstruction()
        {
            base.AfterConstruction();
            // Place your initialization code here (https://docs.devexpress.com/eXpressAppFramework/112834/getting-started/in-depth-tutorial-winforms-webforms/business-model-design/initialize-a-property-after-creating-an-object-xpo?v=22.1).
        }
        void Events_CollectionChanged(object sender, XPCollectionChangedEventArgs e)
        {
            LoadClients();
        }
        //// Inside your ServerNode class, add a method or property like this:
        //public IEnumerable<string> GetDistinctClientNodeIds()
        //{
        //    return Events
        //        .Where(e => !string.IsNullOrEmpty(e.ClientNodeId))
        //        .Select(e => e.ClientNodeId)
        //        .Distinct()
        //        .ToList();




        //}
        //// Returns a dictionary: NodeId -> (latestPush, latestFetch)
        //public static Dictionary<string, (Events? LatestPush, Events? LatestFetch)> GetLatestPushAndFetchPerNode(IQueryable<Events> events)
        //{
        //    // Group events by NodeId
        //    var grouped = events
        //        .Where(e => e.ServerNode != null)
        //        .GroupBy(e => e.ServerNode.NodeId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => (
        //                LatestPush: g
        //                    .Where(e => e.EventType == EventType.Push)
        //                    .OrderByDescending(e => e.Date)
        //                    .ThenByDescending(e => e.Time)
        //                    .FirstOrDefault(),
        //                LatestFetch: g
        //                    .Where(e => e.EventType == EventType.Fetch)
        //                    .OrderByDescending(e => e.Date)
        //                    .ThenByDescending(e => e.Time)
        //                    .FirstOrDefault()
        //            )
        //        );
        //    return grouped;
        //}


        public static BindingList<ClientNode> GetClientNodesWithLatestOperations(IEnumerable<Events> events)
        {
            var result = new BindingList<ClientNode>();

            // Group events by ClientNodeId
            var clientGroups = events
                .Where(e => !string.IsNullOrEmpty(e.ClientNodeId))
                .GroupBy(e => e.ClientNodeId);

            foreach (var clientGroup in clientGroups)
            {
                var clientNodeId = clientGroup.Key;
                var clientNode = new ClientNode { Name = clientNodeId };

                // Find latest push operation
                var latestPush = clientGroup
                    .Where(e => e.EventType == EventType.Push)
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Time)
                    .FirstOrDefault();

                // Find latest fetch operation
                var latestFetch = clientGroup
                    .Where(e => e.EventType == EventType.Fetch)
                    .OrderByDescending(e => e.Date)
                    .ThenByDescending(e => e.Time)
                    .FirstOrDefault();

                // Set the DateTime values
                if (latestPush != null)
                {
                    clientNode.LastPushOperation = new DateTime(
                        latestPush.Date.Year,
                        latestPush.Date.Month,
                        latestPush.Date.Day,
                        latestPush.Time.Hour,
                        latestPush.Time.Minute,
                        latestPush.Time.Second);
                }

                if (latestFetch != null)
                {
                    clientNode.LastFetchOperation = new DateTime(
                        latestFetch.Date.Year,
                        latestFetch.Date.Month,
                        latestFetch.Date.Day,
                        latestFetch.Time.Hour,
                        latestFetch.Time.Minute,
                        latestFetch.Time.Second);
                }

                result.Add(clientNode);
            }

            return result;
        }

        private void LoadClients()
        {

           
            Clients = GetClientNodesWithLatestOperations(this.Events);




          
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            LoadClients();
            this.Events.CollectionChanged += Events_CollectionChanged;
        }

        string name;
        bool active;
        string connectionString;
        string nodeId;

        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string NodeId
        {
            get => nodeId;
            set => SetPropertyValue(nameof(NodeId), ref nodeId, value);
        }
        
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string Name
        {
            get => name;
            set => SetPropertyValue(nameof(Name), ref name, value);
        }
        [Size(SizeAttribute.DefaultStringMappingFieldSize)]
        public string ConnectionString
        {
            get => connectionString;
            set => SetPropertyValue(nameof(ConnectionString), ref connectionString, value);
        }
        
        public bool Active
        {
            get => active;
            set => SetPropertyValue(nameof(Active), ref active, value);
        }
      
        [Association("ServerNode-PersistentPushOperationResponses")]
        public XPCollection<Events> Events
        {
            get
            {
                return GetCollection<Events>(nameof(Events));
            }
        }

        public BindingList<ClientNode> Clients;
        
    }
}