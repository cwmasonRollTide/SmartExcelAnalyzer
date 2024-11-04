using API.Extensions;
using static API.Extensions.ProgramExtensions;

namespace API;

public class Program
{
    public static void Main(string[] _) => WebApplication.CreateBuilder()
        .AddSmartExcelFileAnalyzerVariables()
        .ConfigureLogging()
        .ConfigureMediatR()
        .ConfigureSwagger()
        .ConfigureDatabase()
        .ConfigureServices()
        .ConfigureApiAccess()
        .ConfigureHttpClient()
        .ConfigureLLMService()
        .Build()
        .ConfigureCors()
        .ConfigureMiddleware() 
        .ConfigureProgressHub()
        .Run();
}
