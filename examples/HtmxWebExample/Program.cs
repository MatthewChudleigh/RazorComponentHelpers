using HtmxWebExample.Web;
using HtmxWebExample.Web.Pages;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using RazorComponentHelpers;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ComponentRenderer>();
builder.Services.AddRazorComponents();

StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();
app.MapStaticAssets();

Main.AddEndpoints(app);

app.MapRazorComponents<App>();

try
{
    await app.RunAsync();
}
catch (Exception e)
{
    Log.Logger.Error(e, "Unhandled exception");
}
