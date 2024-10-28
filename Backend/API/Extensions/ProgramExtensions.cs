using System.Text;
using Qdrant.Client;
using API.Properties;
using API.Middleware;
using API.Controllers;
using Persistence.Hubs;
using Application.Queries;
using Application.Services;
using Persistence.Database;
using Persistence.Repositories;
using Microsoft.OpenApi.Models;
using FluentValidation.AspNetCore;
using Domain.Persistence.Configuration;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace API.Extensions;

public static class ProgramExtensions
{
    private const string DefaultClientName = "DefaultClient";
    public static WebApplicationBuilder AddOurEnvironmentVariables(this WebApplicationBuilder? builder)
    {
        builder ??= WebApplication.CreateBuilder();
        builder.Configuration.AddEnvironmentVariables();
        // TODO: KEYVAULT when in Azure
        builder!.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
        return builder;
    }

    public static WebApplicationBuilder ConfigureLogging(this WebApplicationBuilder builder)
    {
        // TODO: APP INSIGHTS when in Azure
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        builder.Logging.ClearProviders();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddConsole();
        builder.Services.AddLogging();
        return builder;
    }

    public static WebApplicationBuilder ConfigureHttpClient(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(DefaultClientName, 
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
        builder.Services.AddScoped<BaseController>();
        var frontendUrl = builder.Configuration["FrontendUrl"];
        builder.Services.AddControllers().AddApplicationPart(typeof(AnalysisController).Assembly);
        builder.Services.AddMvcCore().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(AnalysisController).Assembly));
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                if (!string.IsNullOrEmpty(frontendUrl))
                {
                    builder.WithOrigins(frontendUrl)
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                }
            });
        });

        return builder;
    }

    public static WebApplicationBuilder ConfigureDatabase(this WebApplicationBuilder builder)
    {
        var databaseOptions = builder.Configuration.GetSection("DatabaseOptions");
        builder.Services.Configure<DatabaseOptions>(databaseOptions);
        builder.Services
            .AddOptions<DatabaseOptions>()
            .Validate(options => options.PORT > 0, "Qdrant Port must be set.")
            .Validate(options => options.SAVE_BATCH_SIZE > 0, "Qdrant Save Batch Size must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.HOST), "Qdrant Host String must be set.")
            .Validate(options => options.MAX_CONNECTION_COUNT > 0, "Qdrant Max Connection Count must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.QDRANT_API_KEY), "Qdrant API Key must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.DatabaseName), "Qdrant Database Name must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.CollectionName), "Qdrant Collection Name must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.CollectionNameTwo), "Qdrant Collection Name Two must be set.");
        var options = databaseOptions.Get<DatabaseOptions>();
        builder.Services.AddSingleton(sp => new QdrantClient(
            options!.HOST, 
            options!.PORT, 
            options!.USE_HTTPS, 
            options!.QDRANT_API_KEY, 
            grpcTimeout: TimeSpan.FromMinutes(30)
            )
        );
        builder.Services.AddSingleton<IQdrantClient, QdrantClientWrapper>();
        builder.Services.AddScoped<IDatabaseWrapper, QdrantDatabaseWrapper>();
        builder.Services.AddScoped<IVectorDbRepository, VectorRepository>();
        return builder;
    }
    
    public static WebApplicationBuilder ConfigureLLMService(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ILLMServiceLoadBalancer, LLMLoadBalancer>();
        builder.Services.Configure<LLMServiceOptions>(builder.Configuration.GetSection("LLMServiceOptions"));
        builder.Services
            .AddOptions<LLMServiceOptions>()
            .Validate(options => options.LLM_SERVICE_URLS.Count > 0, "LLM_SERVICE_URLS must be set.")
            .Validate(options => !string.IsNullOrEmpty(options.LLM_SERVICE_URL), "LLM_SERVICE_URL must be set.");
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
        builder.Services.AddSignalR();
        builder.Services.AddScoped<IExcelFileService, ExcelFileService>();
        builder.Services.AddScoped<IProgressHubWrapper, ProgressHubWrapper>();
        builder.Services.AddScoped(typeof(IWebRepository<>), typeof(WebRepository<>));
        return builder;
    }

    public static WebApplicationBuilder ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Excel File Analyzer API", Version = "v1" });
            c.OperationFilter<SwaggerFileOperationFilter>();
        });
        return builder;
    }

    public static WebApplication ConfigureMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment()) app.UseSwagger().UseSwaggerUI().UseDeveloperExceptionPage();

        app.UseCors()
           .UseRouting()
           .UseMiddleware<ExceptionMiddleware>();
        app.MapControllers();
        app.MapHealthChecks("/health");
        return app;
    }

    public static WebApplication ConfigureProgressHub(this WebApplication app)
    {
        app.MapHub<ProgressHub>("/progressHub");
        app.UseCors(builder =>
        {
            var frontendUrl = app.Configuration["FrontendUrl"];
            var frontendUrlString = frontendUrl?.ToString();
            if (!string.IsNullOrEmpty(frontendUrlString))
            {
                builder.WithOrigins(frontendUrlString)
                       .AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowCredentials();
            }
        });
        return app;
    }
}
