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
        // Properties for client nodes access
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
        
        // Connection status tracking
        public bool IsConnected { get; set; } = false;
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            ServerDeltaStore = new MemoryDeltaStore();
            NodeId = "";
            this.RemoteNodeId = "0196e80f-4258-7c11-9896-8fa64a2722b2";
            this.RemoteUrl = "https://localhost:5001/api/SyncFramework/";
            //Subscribe Component to Specific Message
            Bus.Subscribe<object>(RefreshDeltaCount);
            Randomizer.Seed = new Random(8675309);
        }

        // Connection method
        private async Task ConnectAsync()
        {
            if (IsRemoteMode && (string.IsNullOrWhiteSpace(RemoteUrl) || string.IsNullOrWhiteSpace(RemoteNodeId)))
            {
                Snackbar.Add("Please fill both URL and Node ID fields to connect to a remote server", Severity.Warning);
                return;
            }

            try
            {
                if (IsRemoteMode)
                {
                    this.RemoteNodeId = this.RemoteNodeId.Trim();
                    this.NodeId = this.RemoteNodeId.Trim();
                    // Connect to remote server
                    serverComponent.NodeId = RemoteNodeId;
                    serverComponent.Connect(RemoteUrl, RemoteNodeId);
                    Snackbar.Add($"Connected to remote server at {RemoteUrl}", Severity.Success);
                }
                else
                {
                    this.NodeId = "InMemoryServer";
                    serverComponent.NodeId = NodeId;
                    // Connect to in-memory server
                    serverComponent.ConnectToInMemoryServer();
                    Snackbar.Add("Connected to in-memory server", Severity.Success);
                }

                // Set connection state to connected
                IsConnected = true;
                
                // Refresh the delta count after connecting
                DeltaCount = await this.ServerDeltaStore.GetDeltaCountAsync("", NodeId, default);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Connection failed: {ex.Message}", Severity.Error);
                IsConnected = false;
            }
        }
        
        // Disconnect method
        private async Task DisconnectAsync()
        {
            try 
            {
                // Reset connection state
                IsConnected = false;
                NodeId = "";
                Snackbar.Add("Disconnected from server", Severity.Info);
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error during disconnect: {ex.Message}", Severity.Error);
            }
        }

        // Delta count refresh handler
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
            if (!clientNodes.Any())
            {
                Snackbar.Add("No client nodes to download", Severity.Warning);
                return;
            }
            
            try
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

                var zipBytes = FileUtil.CreateZipFromByteArrays(files);
                await FileUtil.SaveAs(js, $"SyncFrameworkPlayGround.zip", zipBytes);
                Snackbar.Add("Databases download initiated", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error downloading databases: {ex.Message}", Severity.Error);
            }
        }
        
        private async void AddClientNode()
        {
            // Validate connection status before adding client node
            if (!IsConnected)
            {
                Snackbar.Add("Please connect to a server first", Severity.Warning);
                return;
            }
            
            try
            {
                NodeCount++;
                string DbName = $"ClientNode_{NodeCount}";
                List<DeltaGeneratorBase> Generators = new List<DeltaGeneratorBase>();

                if (Postgres)
                {
                    Generators.Add(deltaGeneratorBases["Postgres"]);
                }
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
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to add client node: {ex.Message}", Severity.Error);
            }
        }

        public int DeltaCount { get; set; }
    }
}
