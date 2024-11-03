using System.Text;
using Qdrant.Client;
using API.Properties;
using API.Middleware;
using API.Attributes;
using Persistence.Hubs;
using Application.Queries;
using Application.Services;
using Persistence.Database;
using Persistence.Repositories;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using Domain.Persistence.Configuration;

namespace API.Extensions;

public static class ProgramExtensions
{
    public static WebApplicationBuilder AddSmartExcelFileAnalyzerVariables(this WebApplicationBuilder? builder)
    {
        builder ??= WebApplication.CreateBuilder();
        builder!.Configuration.AddJsonFile(Constants.AppSettingsJson, optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile(
            string.Format(Constants.AppSettingsEnvironmentJson, builder.Environment.EnvironmentName), 
            optional: true, 
            reloadOnChange: true);
        builder.Configuration.AddEnvironmentVariables();
        return builder;
    }

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.AddConsole();
        builder.Services.AddLogging();
        builder.Services.AddApplicationInsightsTelemetry();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        builder.Logging.AddConfiguration(builder.Configuration.GetSection(Constants.LoggingSection));
        return builder;
    }

    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(Constants.DefaultClientName, 
            client =>
            {
                client.Timeout = TimeSpan.FromMinutes(30);
            }
        );
        return builder;
    }

    public static WebApplicationBuilder ConfigureApiAccess(this WebApplicationBuilder builder)
    {
        builder.Services.AddSignalR();
        builder.Services.AddHealthChecks();
        builder.Services
            .AddControllers(options => options.AddCommonResponseTypes());
        builder.Services
            .AddFluentValidationAutoValidation()
            .AddFluentValidationClientsideAdapters();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(Constants.DefaultCorsPolicy, builder =>
            {
                builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
            });
        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        var databaseOptions = builder.Configuration.GetSection(Constants.DatabaseOptionsSection);
        builder.Services.Configure<DatabaseOptions>(databaseOptions);
        builder.Services
            .AddOptions<DatabaseOptions>()
            .Validate(options => options.PORT > 0, Constants.ValidationMessages.QdrantPortValidation)
            .Validate(options => options.SAVE_BATCH_SIZE > 0, Constants.ValidationMessages.QdrantBatchSizeValidation)
            .Validate(options => !string.IsNullOrEmpty(options.HOST), Constants.ValidationMessages.QdrantHostValidation)
            .Validate(options => options.MAX_CONNECTION_COUNT > 0, Constants.ValidationMessages.QdrantMaxConnectionValidation)
            .Validate(options => !string.IsNullOrEmpty(options.QDRANT_API_KEY), Constants.ValidationMessages.QdrantApiKeyValidation)
            .Validate(options => !string.IsNullOrEmpty(options.DatabaseName), Constants.ValidationMessages.QdrantDatabaseNameValidation)
            .Validate(options => !string.IsNullOrEmpty(options.CollectionName), Constants.ValidationMessages.QdrantCollectionNameValidation)
            .Validate(options => !string.IsNullOrEmpty(options.CollectionNameTwo), Constants.ValidationMessages.QdrantCollectionNameTwoValidation);
        var options = databaseOptions.Get<DatabaseOptions>();
        builder.Services.AddSingleton(sp => new QdrantClient(
            options!.HOST, 
            options!.PORT, 
            options!.USE_HTTPS, 
            options!.QDRANT_API_KEY, 
            grpcTimeout: TimeSpan.FromMinutes(30))
        );
        builder.Services.AddSingleton<IQdrantClient, QdrantClientWrapper>();
        builder.Services.AddScoped<IDatabaseWrapper, QdrantDatabaseWrapper>();
        builder.Services.AddScoped<IVectorDbRepository, VectorRepository>();
        return builder;
    }
    
    public static WebApplicationBuilder ConfigureLLMService(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ILLMServiceLoadBalancer, LLMLoadBalancer>();
        builder.Services.Configure<LLMServiceOptions>(builder.Configuration.GetSection(Constants.LLMServiceOptionsSection));
        builder.Services
            .AddOptions<LLMServiceOptions>()
            .Validate(options => options.LLM_SERVICE_URLS.Count > 0, Constants.ValidationMessages.LLMServiceUrlsValidation)
            .Validate(options => !string.IsNullOrEmpty(options.LLM_SERVICE_URL), Constants.ValidationMessages.LLMServiceUrlValidation);
        builder.Services.AddScoped<ILLMRepository, LLMRepository>();
        return builder;
    }

    public static WebApplicationBuilder ConfigureMediatR(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddFluentValidationAutoValidation()
            .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SubmitQuery).Assembly));
        return builder;
    }

    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IExcelFileService, ExcelFileService>();
        builder.Services.AddScoped<IProgressHubWrapper, ProgressHubWrapper>();
        builder.Services.AddScoped(typeof(IWebRepository<>), typeof(WebRepository<>));
        return builder;
    }

    public static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                Constants.SwaggerConfig.Version, 
                new OpenApiInfo 
                { 
                    Title = Constants.SwaggerConfig.Title, 
                    Version = Constants.SwaggerConfig.Version 
                });
            c.OperationFilter<SwaggerFileOperationFilter>();
        });
        return builder;
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) 
            app.UseSwagger().UseSwaggerUI().UseDeveloperExceptionPage();

        app.MapControllers();
        app.UseMiddleware<ExceptionMiddleware>();
        app.MapHealthChecks(Constants.HealthCheckEndpoint);
        return app;
    }

    public static WebApplication ConfigureProgressHub(this WebApplication app)
    {
        app.MapHub<ProgressHub>(Constants.ProgressHubEndpoint);
        return app;
    }

    public static WebApplication ConfigureCors(this WebApplication app)
    {
        app.UseCors(Constants.DefaultCorsPolicy);
        return app;
    }

    private static class Constants
    {
        public const string LoggingSection = "Logging";
        public const string HealthCheckEndpoint = "/health";
        public const string DefaultCorsPolicy = "CorsPolicy";
        public const string FrontendUrlConfig = "FrontendUrl";
        public const string DefaultClientName = "DefaultClient";
        public const string AppSettingsJson = "appsettings.json";
        public const string ProgressHubEndpoint = "/progressHub";
        public const string DatabaseOptionsSection = "DatabaseOptions";
        public const string LLMServiceOptionsSection = "LLMServiceOptions";
        public const string AppSettingsEnvironmentJson = "appsettings.{0}.json";
        
        public static class ValidationMessages
        {
            public const string QdrantPortValidation = "Qdrant Port must be set.";
            public const string QdrantApiKeyValidation = "Qdrant API Key must be set.";
            public const string LLMServiceUrlValidation = "LLM_SERVICE_URL must be set.";
            public const string QdrantHostValidation = "Qdrant Host String must be set.";
            public const string LLMServiceUrlsValidation = "LLM_SERVICE_URLS must be set.";
            public const string QdrantBatchSizeValidation = "Qdrant Save Batch Size must be set.";
            public const string QdrantDatabaseNameValidation = "Qdrant Database Name must be set.";
            public const string QdrantCollectionNameValidation = "Qdrant Collection Name must be set.";
            public const string QdrantMaxConnectionValidation = "Qdrant Max Connection Count must be set.";
            public const string QdrantCollectionNameTwoValidation = "Qdrant Collection Name Two must be set.";
        }

        public static class SwaggerConfig
        {
            public const string Version = "v1";
            public const string Title = "Smart Excel File Analyzer API";
        }
    }
}