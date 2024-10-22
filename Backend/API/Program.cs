using API.Extensions;
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
    .ConfigureProgressHub()
    .ConfigureMiddleware()
    .Run();
