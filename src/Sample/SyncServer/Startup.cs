using BIT.Data.Sync;
using BIT.Data.Sync.EfCore;
using BIT.Data.Sync.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using SyncFramework.ConsoleApp.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SyncServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SyncServer", Version = "v1" });
            });
            services.AddDbContext<EfDeltaDbContext>(options => options.UseNpgsql(Configuration.GetConnectionString("DeltaStore")));
            services.AddScoped<EfDeltaDbContext>();
            services.AddScoped<IDeltaStore, EFDeltaStoreExt>();
            services.AddSingleton<IDeltaProcessor, EFDeltaProcessor>(service => new EFDeltaProcessor(service.GetService<EfDeltaDbContext>()));

            services.AddScoped<ISyncServer>(pro =>
            {
                var nodes = Configuration.GetSection("NodeList").Get<string[]>();
                SyncServerNode[] syncServerNodes = new SyncServerNode[nodes.Length];
                for(int i = 0; i < nodes.Length; i++)
                {
                    syncServerNodes[i] = new SyncServerNode(pro.GetService<IDeltaStore>(), null, nodes[i]);
                }
                return new BIT.Data.Sync.Server.SyncServer(syncServerNodes); 
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SyncServer v1"));
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
