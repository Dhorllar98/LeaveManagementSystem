using System.Net;
using LeaveManagement.Domain.Exceptions;

namespace LeaveManagement.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException => ((int)HttpStatusCode.BadRequest, "Validation failed."),
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, exception.Message),
            NotFoundException => ((int)HttpStatusCode.NotFound, exception.Message),
            ConflictException => ((int)HttpStatusCode.Conflict, exception.Message),
            OperationCanceledException or TimeoutException => ((int)HttpStatusCode.GatewayTimeout, "The request timed out."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred on the server.")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled error [TraceId: {TraceId}]", context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning("Handled HTTP {StatusCode} [{ExceptionType}]: {Message}",
                statusCode, exception.GetType().Name, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new
        {
            Success = false,
            Message = message,
            TraceId = context.TraceIdentifier,
            Errors = exception is ValidationException validationEx ? validationEx.Errors : null
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}