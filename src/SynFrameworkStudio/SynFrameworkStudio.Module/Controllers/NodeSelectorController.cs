using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Layout;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Utils;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using SynFrameworkStudio.Module.BusinessObjects;
using SynFrameworkStudio.Module.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SynFrameworkStudio.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public  class NodeSelectorController : ViewController
    {
        SingleChoiceAction ServerNodesAction;
        SimpleAction ConnectToNode;
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/
        public NodeSelectorController()
        {
       
            ServerNodesAction = new SingleChoiceAction(this, "Server Nodes", "View");
            ServerNodesAction.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            ServerNodesAction.Execute += ServerNodesAction_Execute;
            // Create some items
            //ServerNodesAction.Items.Add(new ChoiceActionItem("MyItem1", "My Item 1", 1));
            

            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        private void ServerNodesAction_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            var itemData = e.SelectedChoiceActionItem.Data as ServerNode;


            List<IObjectSpaceProvider> Providers = this.Application.ObjectSpaceProviders.ToList();
            foreach (var provider in Providers)
            {
                if (provider is IConnectToNodeData xpoDataStoreProvider)
                {
                    var connectionString = itemData.ConnectionString;
                    if (!string.IsNullOrEmpty(connectionString))
                    {
                        ((IConnectToNodeData)xpoDataStoreProvider).Connect(connectionString);
                    }
                }
            }


            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112738/).
        }
    
        protected override void OnActivated()
        {
            base.OnActivated();
            var os=this.Application.CreateObjectSpace(typeof(ServerNode));

            os.GetObjectsQuery<ServerNode>().Where(x => x.Active == true).ToList().ForEach(x =>
            {
                ServerNodesAction.Items.Add(new ChoiceActionItem(x.Name, x.NodeId, x));
            });

            // Perform various tasks depending on the target View.
        }
        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();
            // Access and customize the target View control.
        }
        protected override void OnDeactivated()
        {
            // Unsubscribe from previously subscribed events and release other references and resources.
            base.OnDeactivated();
        }
    }
}
