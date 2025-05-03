using MessageFlow.Server.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Set up Serilog before building the app
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        "logs/server-log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

builder.ConfigureApp();
var app = builder.Build();
app.ConfigurePipelineAsync();
app.Run();