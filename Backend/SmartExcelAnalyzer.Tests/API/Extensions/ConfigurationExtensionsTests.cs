using System;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using API.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SmartExcelAnalyzer.Tests.API.Extensions;
public class ConfigurationExtensionsTests
{
    [Fact]
    public void ConfigureEnvironmentVariables_ShouldNotThrowException()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act & Assert
        var exception = Record.Exception(() => builder.ConfigureEnvironmentVariables());
        Assert.Null(exception);
    }

    [Fact]
    public void ConfigureLogging_ShouldAddLoggingServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.ConfigureLogging();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void ConfigureHttpClient_ShouldAddHttpClient()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.ConfigureHttpClient();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void ConfigureApiAccess_ShouldConfigureControllers()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.ConfigureApiAccess();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var corsService = serviceProvider.GetService<ICorsService>();
        Assert.NotNull(corsService);
    }

    // Add more tests for other extension methods as needed
}