using MessageFlow.Server.Configuration;
using Serilog;

namespace MessageFlow.Server;

public class Program
{
    public static void Main(string[] args)
    {
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
    }
}