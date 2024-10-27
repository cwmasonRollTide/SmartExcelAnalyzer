using API.Extensions;

namespace API;

public class Program
{
    public static void Main(string[] _) => 
        ConfigureSmartExcelAnalyzer()
        .ConfigureMiddleware() 
        .ConfigureProgressHub()
        .Run();

    public static WebApplication ConfigureSmartExcelAnalyzer() => 
        WebApplication.CreateBuilder()
        .AddOurEnvironmentVariables()
        .ConfigureLogging()
        .ConfigureMediatR()
        .ConfigureSwagger()
        .ConfigureDatabase()
        .ConfigureServices()
        .ConfigureApiAccess()
        .ConfigureHttpClient()
        .ConfigureLLMService()
        .Build();
}
