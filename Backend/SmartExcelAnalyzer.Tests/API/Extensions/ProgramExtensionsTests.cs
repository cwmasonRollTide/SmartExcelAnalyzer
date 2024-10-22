using FluentAssertions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Persistence.Repositories.API;
using Domain.Persistence.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConfigurationExtensions = API.Extensions.ConfigurationExtensions;
using Microsoft.AspNetCore.Mvc;
using API.Controllers;
using Domain.Persistence.DTOs;
using Swashbuckle.AspNetCore.Swagger;

namespace SmartExcelAnalyzer.Tests.API.Extensions;

public class ProgramExtensionsTests
{
    [Fact]
    public void ConfigureEnvironmentVariables_ShouldAddEnvironmentVariables()
    {
        var builder = WebApplication.CreateBuilder();
        builder = ConfigurationExtensions.ConfigureEnvironmentVariables(builder);

        builder.Configuration["LLMServiceOptions:LLM_SERVICE_URL"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ConfigureLogging_ShouldConfigureLogging()
    {
        var builder = WebApplication.CreateBuilder();
        builder = ConfigurationExtensions.ConfigureLogging(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureHttpClient_ShouldAddHttpClient()
    {
        var builder = WebApplication.CreateBuilder();
        builder = ConfigurationExtensions.ConfigureHttpClient(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

        httpClientFactory.Should().NotBeNull();
    }

    // [Fact]
    // public void ConfigureApiAccess_ShouldConfigureControllers()
    // {
    //     var builder = WebApplication.CreateBuilder();
    //     builder.Services.AddControllers();
    //     builder.Services.AddSingleton<BaseController>();
    //     builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnalysisController).Assembly));
    //     builder = ConfigurationExtensions.ConfigureApiAccess(builder);

    //     var serviceProvider = builder.Services.BuildServiceProvider();
    //     var controllerFeature = serviceProvider.GetService<AnalysisController>();

    //     controllerFeature.Should().NotBeNull();
    // }

    [Fact]
    public void ConfigureDatabase_ShouldConfigureDatabaseOptions()
    {
        var builder = WebApplication.CreateBuilder();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"DatabaseOptions:PORT", "6333"},
                {"DatabaseOptions:SAVE_BATCH_SIZE", "100"},
                {"DatabaseOptions:QDRANT_API_KEY", "test-key"},
                {"DatabaseOptions:HOST", "localhost"},
                {"DatabaseOptions:MAX_CONNECTION_COUNT", "10"},
                {"DatabaseOptions:DatabaseName", "testdb"},
                {"DatabaseOptions:CollectionName", "testcollection"},
                {"DatabaseOptions:CollectionNameTwo", "testcollection2"}
            })
            .Build();

        builder.Configuration.AddConfiguration(configuration);
        builder = ConfigurationExtensions.ConfigureDatabase(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<DatabaseOptions>>();

        options.Should().NotBeNull();
        options!.Value.PORT.Should().Be(6333);
        options!.Value.SAVE_BATCH_SIZE.Should().Be(100);
        options!.Value.QDRANT_API_KEY.Should().Be("test-key");
        options!.Value.HOST.Should().Be("localhost");
        options!.Value.MAX_CONNECTION_COUNT.Should().Be(10);
        options!.Value.DatabaseName.Should().Be("testdb");
        options!.Value.CollectionName.Should().Be("testcollection");
        options!.Value.CollectionNameTwo.Should().Be("testcollection2");
    }

    [Fact]
    public void ConfigureLLMService_ShouldConfigureLLMOptions()
    {
        var builder = WebApplication.CreateBuilder();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"LLMServiceOptions:LLM_SERVICE_URL", "http://localhost:5000"},
                {"LLMServiceOptions:LLM_SERVICE_URLS:0", "http://localhost:5001"},
                {"LLMServiceOptions:LLM_SERVICE_URLS:1", "http://localhost:5002"}
            })
            .Build();

        builder.Configuration.AddConfiguration(configuration);
        builder = ConfigurationExtensions.ConfigureLLMService(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<LLMServiceOptions>>();

        options.Should().NotBeNull();
        options!.Value.LLM_SERVICE_URLS.Should().NotBeEmpty();
        options!.Value.LLM_SERVICE_URLS.Should().HaveCount(6);
        options!.Value.LLM_SERVICE_URLS.Should().Contain("http://localhost:5001");
        options!.Value.LLM_SERVICE_URLS.Should().Contain("http://localhost:5002");
    }

    [Fact]
    public void ConfigureMediatR_ShouldConfigureMediatR()
    {
        var builder = WebApplication.CreateBuilder();
        builder = ConfigurationExtensions.ConfigureMediatR(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var mediator = serviceProvider.GetService<MediatR.IMediator>();

        mediator.Should().NotBeNull();
    }

    [Fact]
    public void ConfigureServices_ShouldAddRequiredServices()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHttpClient();
        builder = ConfigurationExtensions.ConfigureServices(builder);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var excelFileService = serviceProvider.GetService<IExcelFileService>();
        var webRepository = serviceProvider.GetService<IWebRepository<QueryAnswer>>();

        excelFileService.Should().NotBeNull();
        webRepository.Should().NotBeNull();
    }

    // [Fact]
    // public async Task ConfigureSwagger_ShouldConfigureSwagger()
    // {
    //     var builder = WebApplication.CreateBuilder();
    //     builder = ConfigurationExtensions.ConfigureSwagger(builder);

    //     var serviceProvider = builder.Services.BuildServiceProvider();
    //     var swaggerGenerator = serviceProvider.GetService<IAsyncSwaggerProvider>();

    //     swaggerGenerator.Should().NotBeNull();
    // }

    // [Fact]
    // public void ConfigureMiddleware_ShouldConfigureMiddleware()
    // {
    //     var builder = WebApplication.CreateBuilder();
    //     var app = builder.Build();
    //     app = ConfigurationExtensions.ConfigureMiddleware(app);
    //     app.Should().NotBeNull();
    // }
}