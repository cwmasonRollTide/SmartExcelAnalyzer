using MediatR;
using FluentAssertions;
using Persistence.Hubs;
using Application.Services;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using SmartExcelAnalyzer.Tests.TestUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace SmartExcelAnalyzer.Tests.API;

public class ProgramTests(CustomWebApplicationFactory _factory) : IClassFixture<CustomWebApplicationFactory>
{

    [Fact]
    public void ConfigureServices_ShouldRegisterRequiredServices()
    {
        var services = _factory.Services;

        services
            .GetService<IExcelFileService>()
            .Should()
            .NotBeNull();
        services
            .GetService<ILLMRepository>()
            .Should()
            .NotBeNull();
        services
            .GetService<IVectorDbRepository>()
            .Should()
            .NotBeNull();
        services
            .GetService<IProgressHubWrapper>()
            .Should()
            .NotBeNull();
        services
            .GetService<IWebRepository<QueryAnswer>>()
            .Should()
            .NotBeNull();
        services
            .GetService<IMediator>()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Development_Environment_ShouldConfigureSwagger()
    {
        var services = _factory.Services;

        services
            .GetService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>()
            .Should()
            .NotBeNull();
    }

    [Fact]
    public void Middleware_ShouldBeConfiguredCorrectly()
    {
        var services = _factory.Services;

        var corsOptions = services.GetService<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider>();
        corsOptions.Should().NotBeNull();
        
        var hubLifetimeManager = services.GetService<IHubContext<ProgressHub>>();
        hubLifetimeManager.Should().NotBeNull();
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldHandleErrors()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/non-existent-endpoint");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public void EnvironmentVariables_ShouldBeLoaded()
    {
        var services = _factory.Services;
        var configuration = services.GetService<IConfiguration>();

        configuration.Should().NotBeNull();
        configuration!["LLMServiceOptions:LLM_SERVICE_URL"].Should().NotBeNullOrEmpty();
        configuration["DatabaseOptions:QDRANT_HOST"].Should().NotBeNullOrEmpty();
        configuration["DatabaseOptions:QDRANT_PORT"].Should().NotBeNullOrEmpty();
        configuration["DatabaseOptions:HOST"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MediatR_ShouldBeConfigured()
    {
        var services = _factory.Services;
        var mediator = services.GetService<IMediator>();
        mediator.Should().NotBeNull();
    }
}
