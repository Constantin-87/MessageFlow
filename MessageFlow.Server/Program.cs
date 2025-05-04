using MessageFlow.Server.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApp();
var app = builder.Build();
app.ConfigurePipelineAsync();
try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}