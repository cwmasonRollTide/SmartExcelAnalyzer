using API;
using FluentAssertions;
using Persistence.Hubs;
using Application.Services;
using Persistence.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace SmartExcelAnalyzer.Tests.API;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureWebHost(webHost =>
        {
            webHost.UseTestServer(options => options.PreserveExecutionContext = true);
            webHost.ConfigureKestrel(options => options.ListenLocalhost(0));
            webHost.UseEnvironment("Development");
            webHost.UseStartup<Program>();
            webHost.Configure(app =>
            {
                app.UseRouting();
                app.UseCors();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks("/health");
                    endpoints.MapHub<ProgressHub>("/progressHub");
                });
            });
        });
        return base.CreateHost(builder);
    }
}

public class ProgramTests(CustomWebApplicationFactory _factory) : IClassFixture<CustomWebApplicationFactory>
{

    [Fact]
    public void ConfigureServices_ShouldRegisterRequiredServices()
    {
        var client = _factory.CreateClient();
        var services = _factory.Services;

        services.GetService<IExcelFileService>().Should().NotBeNull();
        services.GetService<ILLMRepository>().Should().NotBeNull();
        services.GetService<IProgressHubWrapper>().Should().NotBeNull();
    }

    [Fact]
    public void Development_Environment_ShouldConfigureSwagger()
    {
        var services = _factory.Services;

        services.GetService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>()
            .Should().NotBeNull();
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
        var configuration = services.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        configuration.Should().NotBeNull();
        configuration!["LLMServiceOptions:LLM_SERVICE_URL"].Should().NotBeNullOrEmpty();
        configuration["DatabaseOptions:HOST"].Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MediatR_ShouldBeConfigured()
    {
        var services = _factory.Services;
        var mediator = services.GetService<MediatR.IMediator>();

        mediator.Should().NotBeNull();
    }
}
