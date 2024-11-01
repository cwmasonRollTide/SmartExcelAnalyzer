using Moq;
using System.Net;
using SmartExcelAnalyzer.Tests.TestUtilities;
using API.Middleware;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SmartExcelAnalyzer.Tests.API.Middleware;

public class ExceptionMiddlewareTests
{
    private Mock<ILogger<ExceptionMiddleware>> _loggerMock = new();

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        var nextDelegateCalled = false;
        Task next(HttpContext httpContext)
        {
            nextDelegateCalled = true;
            return Task.CompletedTask;
        }

        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);

        await middleware.InvokeAsync(context);

        nextDelegateCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ExceptionThrown_ReturnsInternalServerError()
    {
        const string exceptionMessage = "An unhandled exception occurred.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new Exception(exceptionMessage);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("500");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_ValidationExceptionThrown_ReturnsValidationError()
    {
        const string exceptionMessage = "A validation exception occurred.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new ValidationException(exceptionMessage);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("400");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_TaskCanceledExceptionThrown_ReturnsRequestTimeout()
    {
        const string exceptionMessage = "A task was canceled.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new TaskCanceledException(exceptionMessage);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status408RequestTimeout);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("408");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_TimeoutExceptionThrown_ReturnsRequestTimeout()
    {
        const string exceptionMessage = "A timeout occurred.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new TimeoutException(exceptionMessage);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status408RequestTimeout);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("408");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_HttpRequestExceptionThrown_ReturnsCorrectStatusCode()
    {
        const string exceptionMessage = "An HTTP request exception occurred.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new HttpRequestException(exceptionMessage, null, HttpStatusCode.BadGateway);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("502");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceledExceptionThrown_ReturnsRequestTimeout()
    {
        const string exceptionMessage = "An operation was canceled.";
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        static Task next(HttpContext httpContext)
        {
            throw new OperationCanceledException(exceptionMessage);
        }
        var middleware = new ExceptionMiddleware(next, _loggerMock.Object);
        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status408RequestTimeout);
        context.Response.ContentType.Should().Contain("application/json");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
        responseBody.Should().Contain("408");
        _loggerMock.VerifyLog(LogLevel.Error, exceptionMessage);
    }
}
