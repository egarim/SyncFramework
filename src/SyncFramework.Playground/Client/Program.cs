using BIT.Data.Sync.EfCore.Npgsql;
using BIT.Data.Sync.EfCore.Pomelo.MySql;
using BIT.Data.Sync.EfCore.SQLite;
using BIT.Data.Sync.EfCore.SqlServer;
using BIT.EfCore.Sync;
using BlazorComponentBus;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

namespace SyncFramework.Playground.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            MySqlServerVersion serverVersion;
            serverVersion = new MySqlServerVersion(new Version(8, 0, 31));
            Dictionary<string, DeltaGeneratorBase> DeltaGenerators = new Dictionary<string, DeltaGeneratorBase>();
      
            DeltaGenerators.Add("Postgres",new NpgsqlDeltaGenerator());
            DeltaGenerators.Add("MySQL",new PomeloMySqlDeltaGenerator(serverVersion));
            //HACK not needed the main db is Sqlite anyways
            //DeltaGenerators.Add("SQLite",new SqliteDeltaGenerator());
            DeltaGenerators.Add("SqlServer",new SqlServerDeltaGenerator());
            
            builder.Services.AddSingleton<Dictionary<string, DeltaGeneratorBase>>(DeltaGenerators);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<ComponentBus>();
            builder.Services.AddMudServices();
            await builder.Build().RunAsync();
        }
    }
}