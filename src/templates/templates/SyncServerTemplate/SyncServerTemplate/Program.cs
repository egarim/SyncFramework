using BIT.Data.Sync.Imp;
using BIT.Data.Sync.Server;

namespace SyncServerTemplate
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            ////HACK you can add as many nodes as you want and then add a single SyncServer to the DI container

            //if you want to use EfCore database as DeltaStore add this package BIT.Data.Sync.EfCore

            //Memory Delta Store Registration
            //SyncServerNode MemoryServerNode = new SyncServerNode(new MemoryDeltaStore(), null, "MemoryDeltaStore1");


            //EfDeltaStore Registration
            //SyncServerNode EfServerNode = new SyncServerNode(new EFDeltaStore(), null, "EfDeltaStore1");

            //builder.Services.AddSyncServerWithNodes(MemoryServerNode,EfServerNode);

            ////HACK or you can use the extension method AddSyncServerWithMemoryNode
            builder.Services.AddSyncServerWithMemoryNode("MemoryDeltaStore1");


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}