using API.Extensions;

namespace API;

public class Program
{
    public static void Main(string[] _) => ConfigureSmartExcelAnalyzer().Run();

    public static WebApplication ConfigureSmartExcelAnalyzer() => 
        WebApplication.CreateBuilder()
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
}
