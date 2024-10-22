using System.Net;
using FluentValidation;

namespace API.Middleware;

public class ExceptionMiddleware(RequestDelegate _next, ILogger<ExceptionMiddleware> _logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            _logger.LogError(validationException, "A validation exception occurred.");
            await HandleExceptionAsync(context, validationException, HttpStatusCode.BadRequest);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            _logger.LogError(taskCanceledException, "A task was canceled.");
            await HandleExceptionAsync(context, taskCanceledException, HttpStatusCode.RequestTimeout);
        }
        catch (TimeoutException timeoutException)
        {
            _logger.LogError(timeoutException, "A timeout occurred.");
            await HandleExceptionAsync(context, timeoutException, HttpStatusCode.RequestTimeout);
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogError(httpRequestException, "An HTTP request exception occurred.");
            await HandleExceptionAsync(context, httpRequestException, httpRequestException.StatusCode!.Value);
        }
        catch (OperationCanceledException operationCanceledException)
        {
            _logger.LogError(operationCanceledException, "An operation was canceled.");
            await HandleExceptionAsync(context, operationCanceledException, HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsJsonAsync(new
        {
            exception.Message,
            context.Response.StatusCode
        });
    }
}