using BIT.Data.Sync.Server;
using BIT.Data.Sync.Imp;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

// Register sync server with a default memory node
builder.Services.AddSyncServerWithMemoryNode("default");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

if (app.Environment.IsDevelopment())
    app.MapStaticAssets();
else
    app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<SyncFramework.Dashboard.Components.App>()
   .AddInteractiveServerRenderMode();

app.Run();
