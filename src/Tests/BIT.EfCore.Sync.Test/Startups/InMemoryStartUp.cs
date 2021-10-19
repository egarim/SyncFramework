using BIT.Data.Sync.Options;
using BIT.Data.Sync.Server;
using BIT.Data.Sync.Server.Extensions;
using BIT.Data.Sync.TextImp;
using BIT.EfCore.Sync.DeltaProcessors;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace BIT.EfCore.Sync.Test.Startups
{
    public class InMemoryStartUp
    {
        public InMemoryStartUp(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();


            IConfigurationSection ConfigMemoryDeltaStore1 = Configuration.GetSection("DeltaStore:MemoryDeltaStore1");
            IConfigurationSection ConfigMemoryDeltaStore2 = Configuration.GetSection("DeltaStore:MemoryDeltaStore2");

            services.Configure<DeltaStoreSettings>("MemoryDeltaStore1", ConfigMemoryDeltaStore1);
            services.Configure<DeltaStoreSettings>("MemoryDeltaStore2", ConfigMemoryDeltaStore2);

            List<DeltaStoreConfigurationOptions> DeltaStores = new List<DeltaStoreConfigurationOptions>();
            DeltaStoreConfigurationOptions MemoryDeltaStore1 = new DeltaStoreConfigurationOptions(typeof(EFDeltaStore), "MemoryDeltaStore1");
            DeltaStoreConfigurationOptions MemoryDeltaStore2 = new DeltaStoreConfigurationOptions(typeof(MemoryDeltaStore), "MemoryDeltaStore2");

            DeltaStores.Add(MemoryDeltaStore1);
            DeltaStores.Add(MemoryDeltaStore2);

            List<DeltaStoreConfigurationOptions> DeltaProcessors = new List<DeltaStoreConfigurationOptions>();
            DeltaStoreConfigurationOptions MemoryDeltaProcessor1 = new DeltaStoreConfigurationOptions(typeof(MemoryDeltaProcessor), "MemoryDeltaStore1");
            DeltaStoreConfigurationOptions MemoryDeltaProcessor2 = new DeltaStoreConfigurationOptions(typeof(EFDeltaProcessor), "MemoryDeltaStore2");
            DeltaProcessors.Add(MemoryDeltaProcessor1);
            DeltaProcessors.Add(MemoryDeltaProcessor2);
            services.AddDataStoreTypes(DeltaStores.ToArray(), DeltaProcessors.ToArray());


            // Add named options configuration AFTER other configuration
            //services.AddSingleton<IConfigureOptions<DeltaStoreSettings>, DataStoreRegistrationService>();



            //Consumer
            //services.AddScoped<SlackNotificationService>();
            services.AddScoped<ISyncServerNode, SyncServerBase>();
            //services.AddSyncServer()
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
