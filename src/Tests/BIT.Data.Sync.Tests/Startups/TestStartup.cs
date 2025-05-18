
using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace BIT.Data.Sync.Tests.Startups
{
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public static string STR_MemoryDeltaStore1 = "MemoryDeltaStore1";
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();


            services.AddSyncServerWithMemoryNode(STR_MemoryDeltaStore1, RegisterNewNode);
            

        }
        ISyncServerNode RegisterNewNode(RegisterNodeRequest request)
        {
            string NodeId = request.Options.FirstOrDefault(k => k.Key == "NodeId").Value.ToString();
            return new SyncServerNode(new MemoryDeltaStore(), null, NodeId);
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
