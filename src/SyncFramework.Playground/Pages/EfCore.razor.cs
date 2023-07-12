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
using SyncFramework.Playground.EfCore;
using SyncFramework.Playground.Shared;
using System;
using System.Net.Http;
using System.Security.Principal;
using static MudBlazor.CategoryTypes;
using Person = SyncFramework.Playground.EfCore.Person;

namespace SyncFramework.Playground.Pages
{
    public partial class EfCore
    {
    
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
            ServerDeltaStore = new MemoryDeltaStore();
            NodeId = "MainServer";
            //Subscribe Component to Specific Message
            Bus.Subscribe<object>(RefreshDeltaCount);
            Randomizer.Seed = new Random(8675309);

        }
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

            var NodeInstance=new EfClientNodeInstance(js, DbName,Bus, DbName,this.serverComponent.HttpClient,this.serverComponent.NodeId, Generators.ToArray(), this.GenerateRandomData);
            this.clientNodes.Add(NodeInstance);

            Snackbar.Add($"New client added: {DbName}", Severity.Success);

        }



 
        public int DeltaCount { get; set; }
    }
}
