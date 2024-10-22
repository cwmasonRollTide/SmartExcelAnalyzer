using API.Extensions;
using Persistence.Hubs;

namespace API;

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConfigureServices();
        ConfigureMiddleware(app);
        app.Run();
    }

    public static WebApplication ConfigureServices() => WebApplication.CreateBuilder()
        .ConfigureEnvironmentVariables()
        .ConfigureLogging()
        .ConfigureMediatR()
        .ConfigureSwagger()
        .ConfigureDatabase()
        .ConfigureServices()
        .ConfigureApiAccess()
        .ConfigureHttpClient()
        .ConfigureLLMService()
        .Build();

    public static WebApplication ConfigureMiddleware(WebApplication app)
    {
        app.UseCors();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapHub<ProgressHub>("/progressHub");
        });
        return app;
    }
}
