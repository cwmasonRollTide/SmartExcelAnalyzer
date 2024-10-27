using API.Extensions;

namespace API;

public class Program
{
    public static void Main(string[] _) => WebApplication.CreateBuilder()
        .AddOurEnvironmentVariables()
        .ConfigureLogging()
        .ConfigureMediatR()
        .ConfigureSwagger()
        .ConfigureDatabase()
        .ConfigureServices()
        .ConfigureApiAccess()
        .ConfigureHttpClient()
        .ConfigureLLMService()
        .Build()
        .ConfigureMiddleware() 
        .ConfigureProgressHub()
        .Run();
}
