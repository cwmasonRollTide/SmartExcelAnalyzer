using System.Text;
using API.Extensions;
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
WebApplication
    .CreateBuilder(args)
    .ConfigureEnvironmentVariables()
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
    .Run();
