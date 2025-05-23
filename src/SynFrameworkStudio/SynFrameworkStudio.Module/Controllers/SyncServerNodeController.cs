using BIT.Data.Sync;
using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.Pomelo.MySql;
using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.SqlServer;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SynFrameworkStudio.Module.BusinessObjects;
using SynFrameworkStudio.Module.BusinessObjects.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SynFrameworkStudio.Module.Controllers
{
    // For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.ViewController.
    public partial class ServerNodeController : ViewController
    {
        SimpleAction PurgeDeltas;
        SimpleAction Test;
        PopupWindowShowAction ShowConfig;
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

            ShowConfig = new PopupWindowShowAction(this, "Show Config", "View");
            ShowConfig.Execute += ShowConfig_Execute;
            ShowConfig.CustomizePopupWindowParams += ShowConfig_CustomizePopupWindowParams;

            Test = new SimpleAction(this, "Test", "View");
            Test.Execute += Test_Execute;

            PurgeDeltas = new SimpleAction(this, "Purge Deltas", "View");
            PurgeDeltas.Execute += PurgeDeltas_Execute;
            

            // Target required Views (via the TargetXXX properties) and create their Actions.
        }
        private async void PurgeDeltas_Execute(object sender, SimpleActionExecuteEventArgs e)
        {
            var SyncServer = this.Application.ServiceProvider.GetService(typeof(ISyncServer)) as ISyncServer;


            try
            {
                await SyncServer.Nodes.FirstOrDefault().DeltaStore.PurgeDeltaStoreAsync(default);
                var currentNode = this.View.CurrentObject as ServerNode;
                this.View.ObjectSpace.Delete(currentNode.Events);
                this.View.ObjectSpace.CommitChanges();
            }
            catch (Exception)
            {

                throw;
            }

          
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Async/await", "CRR0033:The void async method should be in a try/catch block", Justification = "<Pending>")]
        private  async void Test_Execute(object sender, SimpleActionExecuteEventArgs e)
        {


            DbContextOptionsBuilder<TestDbContext> ContextBuilder = new DbContextOptionsBuilder<TestDbContext>();


            string tempDbPath = Path.Combine(Path.GetTempPath(), "EfSqlite_Data.db");

            if(File.Exists(tempDbPath))
            {
                File.Delete(tempDbPath);
            }

            ContextBuilder.UseSqlite($"Data Source={tempDbPath}");

            //ContextBuilder.UseSqlite("Data Source=EfSqlite_Data.db");

            var serverVersion = new MySqlServerVersion(new Version(8, 0, 31));

            List<DeltaGeneratorBase> DeltaGenerators = new List<DeltaGeneratorBase>();
            DeltaGenerators.Add(new NpgsqlDeltaGenerator());
            DeltaGenerators.Add(new PomeloMySqlDeltaGenerator(serverVersion));
            DeltaGenerators.Add(new SqliteDeltaGenerator());
            DeltaGenerators.Add(new SqlServerDeltaGenerator());
            DeltaGeneratorBase[] additionalDeltaGenerators = DeltaGenerators.ToArray();

            ServiceCollection serviceDescriptors = new ServiceCollection();
            HttpClient httpClient = new HttpClient();
            //https://localhost:5001/api/SyncFramework
            
            var CurrentServerNode=  this.View.CurrentObject as ServerNode;

            httpClient.BaseAddress = new Uri("https://localhost:5001/api/SyncFramework/");

            string tempDeltaDbPath = Path.Combine(Path.GetTempPath(), "EfSqlite_Deltas.db");


            if (File.Exists(tempDeltaDbPath))
            {
                File.Delete(tempDeltaDbPath);
            }

            var DeltaCnx = $"Data Source={tempDeltaDbPath}";
            serviceDescriptors.AddSyncFrameworkForSQLite(DeltaCnx, httpClient, CurrentServerNode.NodeId, "Test", additionalDeltaGenerators);

            YearSequencePrefixStrategy implementationInstance = new YearSequencePrefixStrategy();
            serviceDescriptors.AddSingleton(typeof(ISequencePrefixStrategy), implementationInstance);
            serviceDescriptors.AddSingleton(typeof(ISequenceService), typeof(EfSequenceService));
            var provider = serviceDescriptors.BuildServiceProvider();

           
            var Context = new TestDbContext(ContextBuilder.Options, provider);


            Context.Database.EnsureDeleted();
            Context.Database.EnsureCreated();




            Data data = new Data();
            data.Text = "this is a test delta";
            Context.DataRecords.Add(data);
            await Context.SaveChangesAsync();

            var Result=await Context.PushAsync();
            var Fetch= await Context.FetchAsync();
            Context.Database.EnsureDeleted();
            this.Frame.GetController<RefreshController>().RefreshAction.DoExecute();
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112737/).

        }
        private void ShowConfig_Execute(object sender, PopupWindowShowActionExecuteEventArgs e)
        {
            var selectedPopupWindowObjects = e.PopupWindowViewSelectedObjects;
            var selectedSourceViewObjects = e.SelectedObjects;
            // Execute your business logic (https://docs.devexpress.com/eXpressAppFramework/112723/).
        }
        private void ShowConfig_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {
            var Os = this.Application.CreateObjectSpace(typeof(TextObject));
            var registerNodeRequestParameters = Os.CreateObject<TextObject>();
            // Set the e.View parameter to a newly created view (https://docs.devexpress.com/eXpressAppFramework/112723/).
            registerNodeRequestParameters.Content = "Config";

            e.View = Application.CreateDetailView(Os, registerNodeRequestParameters);
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
                SyncServer.CreateNodeAsync(registerNodeRequest);

                var Node = this.ObjectSpace.CreateObject<ServerNode>();
                Node.NodeId = Parameters.Id;
                Node.Name = Parameters.Name;
                Node.ConnectionString = Parameters.ConnectionString;
                Node.Active = true;
                this.ObjectSpace.CommitChanges();
            }


        }
        private void RegisterNode_CustomizePopupWindowParams(object sender, CustomizePopupWindowParamsEventArgs e)
        {


         

            var Os = this.Application.CreateObjectSpace(typeof(RegisterNodeRequestParameters));
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
