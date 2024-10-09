using System.Text;
using API.Extensions;

var builder = WebApplication.CreateBuilder(args);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.ConfigureEnvironmentVariables()
       .ConfigureLogging()
       .ConfigureHttpClient()
       .ConfigureApiAccess()
       .ConfigureDatabase()
       .ConfigureLLMService()
       .ConfigureMediatR()
       .ConfigureServices()
       .ConfigureSwagger();

var app = builder.Build();
app.ConfigureMiddleware();
app.Run();
