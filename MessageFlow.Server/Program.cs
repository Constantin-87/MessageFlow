using MessageFlow.Server.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureApp();
var app = builder.Build();
await app.ConfigurePipelineAsync();
app.Run();