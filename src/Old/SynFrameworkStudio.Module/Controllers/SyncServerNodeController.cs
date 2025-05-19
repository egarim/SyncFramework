using BIT.Data.Sync.Server;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SynFrameworkStudio.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public partial class ServerNodeController : ViewController
    {
        PopupWindowShowAction RegisterNode;
        SimpleAction RegisterNodeOld;
        // Use CodeRush to create Controllers and Actions with a few keystrokes.
        // https://docs.devexpress.com/CodeRushForRoslyn/403133/
        public ServerNodeController()
        {
            InitializeComponent();
            this.TargetObjectType = typeof(ServerNode);

          

            RegisterNode = new PopupWindowShowAction(this, "Register Node", "View");
            RegisterNode.Execute += RegisterNode_Execute;
            RegisterNode.CustomizePopupWindowParams += RegisterNode_CustomizePopupWindowParams;
            RegisterNode.Caption = "Register Node";

            

            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        private void RegisterNode_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var Parameters = e.PopupWindowViewSelectedObjects[0] as RegisterNodeRequestParameters;



            var SyncServer = this.Application.ServiceProvider.GetService(typeof(ISyncServer)) as ISyncServer;

            if (SyncServer != null)
            {
                RegisterNodeRequest registerNodeRequest = new() { };
                registerNodeRequest.Options.Add(new Option("NodeId", Parameters.Id));
                registerNodeRequest.Options.Add(new Option(nameof(Parameters.ConnectionString), Parameters.ConnectionString));
                registerNodeRequest.Options.Add(new Option(nameof(Application), this.Application));
                SyncServer.RegisterNodeAsync(registerNodeRequest);

                var Node=this.ObjectSpace.CreateObject<ServerNode>();
                Node.NodeId = Parameters.Id;
                Node.Name = Parameters.Name;
                Node.ConnectionString = Parameters.ConnectionString;
                Node.Active = true;
                this.ObjectSpace.CommitChanges();
            }


        }
        private void RegisterNode_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var Os=  this.Application.CreateObjectSpace(typeof(RegisterNodeRequestParameters));
            var registerNodeRequestParameters = Os.CreateObject<RegisterNodeRequestParameters>();
            // Set the e.View parameter to a newly created view (https://docs.devexpress.com/eXpressAppFramework/112723/).
            e.View = Application.CreateDetailView(Os, registerNodeRequestParameters);
        }
        private void RegisterNode_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
           
        }
        protected override void OnActivated()
        {
            base.OnActivated();
           
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
