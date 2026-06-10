// Backend purpose: Converts application and unexpected exceptions into consistent HTTP responses.
using System.Net;
using System.Text.Json;
using BookingSystem.Application.Exceptions;

namespace API.ExceptionMiddleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    // The middleware receives the next pipeline step and a logger for unexpected failures.
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Continue through the pipeline when the request can be handled normally.
            await _next(context);
        }
        catch (AppException ex)
        {
            // Controlled application errors are safe to return as client-facing bad requests.
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { message = ex.Message });
            await context.Response.WriteAsync(result);
        }
        catch(Exception ex)
        {
            // Unexpected errors are logged before a generic server error response is written.
            _logger.LogError(ex, "Unhandled exception occurred.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new 
                { error = ex.Message, 
                    details = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                    
                });
            await context.Response.WriteAsync(result);
        }
    }
}

