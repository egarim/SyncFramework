
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace BIT.Data.Sync.EfCore.Tests.Startups
{
    public class TestStartup
    {
        public static string STR_MemoryDeltaStore1 = "MemoryDeltaStore1";
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();


            ////HACK you can add as many nodes as you want and then add a single SyncServer to the DI container
            //SyncServerNode syncServerNode = new SyncServerNode(new MemoryDeltaStore(), null, "MemoryDeltaStore1");
            //services.AddSingleton<ISyncServer>(new SyncServer(syncServerNode));

            ////HACK or you can use the extension method AddSyncServerWithMemoryNode
            services.AddSyncServerWithMemoryNode(STR_MemoryDeltaStore1);

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
