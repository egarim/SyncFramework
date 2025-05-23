using BIT.Data.Sync;
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using BIT.EfCore.Sync;
using BlazorComponentBus;
using Bogus;
using Microsoft.AspNetCore.Components;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using SyncFramework.Playground.Components;
using SyncFramework.Playground.Components.Interfaces;
using SyncFramework.Playground.EfCore;
using SyncFramework.Playground.Shared;
using System;
using System.Net.Http;
using System.Security.Principal;
using static MudBlazor.CategoryTypes;

namespace SyncFramework.Playground.Pages
{
    public partial class EfCore
    {
      
        
        // Existing properties
        public IQueryable<IClientNodeInstance> ClientNodes
        {
            get
            {
                return clientNodes.AsQueryable();
            }
        }
        
        [Inject]
        BlazorComponentBus.ComponentBus Bus { get; set; }
        [Inject]
        public IDialogService DialogService { get; set; }
        [Inject]
        private IJSRuntime js { get; set; }
        [Inject]
        public Dictionary<string, DeltaGeneratorBase> deltaGeneratorBases { get; set; }
        public IDeltaStore ServerDeltaStore { get; set; }
        public string NodeId { get; set; }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            // For debugging only
            Console.WriteLine("Component initialized, IsRemoteMode: " + IsRemoteMode);
            
            //DbContextOptionsBuilder ServerDbContextOptions = new DbContextOptionsBuilder();
            //var ServerCnx = $"Data Source=Server_Deltas.db";
            //ServerDbContextOptions.UseSqlite(ServerCnx);
            //EfDeltaStore efDeltaStore = new EfDeltaStore(new DeltaDbContext(ServerDbContextOptions.Options));
            ServerDeltaStore = new MemoryDeltaStore();
            NodeId = "MainServer";
            //Subscribe Component to Specific Message
            Bus.Subscribe<object>(RefreshDeltaCount);
            Randomizer.Seed = new Random(8675309);
        }
        
        // New method for remote connection
        private async Task ConnectAsync()
        {
            if (IsRemoteMode && (string.IsNullOrWhiteSpace(RemoteUrl) || string.IsNullOrWhiteSpace(RemoteNodeId)))
            {
                Snackbar.Add("Please fill both URL and Node ID fields to connect to a remote server", Severity.Warning);
                return;
            }

            var mode = IsRemoteMode
                ? $"Remote Server (URL: {RemoteUrl}, NodeId: {RemoteNodeId})"
                : "In-Memory Database";
                
            Snackbar.Add($"Connecting to {mode}...", Severity.Info);
            await Task.Delay(500); // Simulate async work
            
            // TODO: Implement actual connection logic based on IsRemoteMode
            // If remote mode, connect to the remote server
            // If in-memory mode, use local memory store (already set up in OnInitialized)
        }
        
        // Existing methods
        private async void RefreshDeltaCount(MessageArgs args)
        {
            var message = args.GetMessage<object>();
            DeltaCount = await this.ServerDeltaStore.GetDeltaCountAsync("", NodeId, default);
            this.StateHasChanged();
        }
        
        public bool GenerateRandomData { get; set; } = true;
        SyncServerComponent serverComponent;
        private List<IClientNodeInstance> clientNodes = new List<IClientNodeInstance>();

        private int NodeCount;
        public bool Postgres { get; set; } = true;
        public bool Sqlite { get; set; } = true;
        public bool SqlServer { get; set; } = true;
        public bool MySql { get; set; } = true;
        
        private async void DownloadAllDatabases()
        {
            Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
            string DataFileName = "";
            string DeltaFileName = "";
            foreach (IClientNodeInstance clientNodeInstance in this.clientNodes)
            {
                DataFileName = $"{clientNodeInstance.Id}_Data.db";
                DeltaFileName = $"{clientNodeInstance.Id}_Deltas.db";
                var DataDbBytes = File.ReadAllBytes(DataFileName);
                var DeltasDbBytes = File.ReadAllBytes(DeltaFileName);
                files.Add(DataFileName, DataDbBytes);
                files.Add(DeltaFileName, DeltasDbBytes);
            }
            //var ServerDeltasBytes = File.ReadAllBytes("Server_Deltas.db");
            //files.Add(DataFileName, ServerDeltasBytes);

            var zipBytes = FileUtil.CreateZipFromByteArrays(files);
            await FileUtil.SaveAs(js, $"SyncFrameworkPlayGround.zip", zipBytes);
        }
        
        private async void AddClientNode()
        {
            NodeCount++;
            string DbName = $"ClientNode_{NodeCount}";
            List<DeltaGeneratorBase> Generators = new List<DeltaGeneratorBase>();

            if (Postgres)
            {
                Generators.Add(deltaGeneratorBases["Postgres"]);
            }
            //if (Sqlite)
            //{
            //    Generators.Add(deltaGeneratorBases["SQLite"]);
            //}
            if (MySql)
            {
                Generators.Add(deltaGeneratorBases["MySQL"]);
            }
            if (SqlServer)
            {
                Generators.Add(deltaGeneratorBases["SqlServer"]);
            }

            var NodeInstance = new EfClientNodeInstance(js, DbName, Bus, DbName, this.serverComponent.HttpClient, this.serverComponent.NodeId, Generators.ToArray(), this.GenerateRandomData);
            this.clientNodes.Add(NodeInstance);

            Snackbar.Add($"New client added: {DbName}", Severity.Success);
        }

        public int DeltaCount { get; set; }
    }
}
