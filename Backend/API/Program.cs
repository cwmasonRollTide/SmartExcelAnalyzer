using System.Text;
using API.Extensions;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

WebApplication
	.CreateBuilder(args)
	.ConfigureEnvironmentVariables()
	.ConfigureLogging()
	.ConfigureHttpClient()
	.ConfigureApiAccess()
	.ConfigureDatabase()
	.ConfigureLLMService()
	.ConfigureMediatR()
	.ConfigureServices()
	.ConfigureSwagger()
	.Build()
	.ConfigureMiddleware()
	.Run();
