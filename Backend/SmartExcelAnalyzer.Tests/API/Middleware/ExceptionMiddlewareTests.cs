using Moq;
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(exceptionMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
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
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(exceptionMessage)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}