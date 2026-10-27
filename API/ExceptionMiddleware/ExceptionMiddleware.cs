// Backend purpose: Converts application and unexpected exceptions into consistent HTTP responses
// without exposing internals (messages, stack traces, SQL details) outside Development.
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using BookingSystem.Application.Exceptions;
using FluentValidation;

namespace API.ExceptionMiddleware;

public class ExceptionMiddleware
{
    public const string GenericErrorMessage = "Ett oväntat fel inträffade. Ange felkoden om du kontaktar support.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer and nothing to log as an error.
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        object body;
        HttpStatusCode status;

        switch (exception)
        {
            case AppException appException:
                // Controlled application errors are written for the user and safe to return.
                status = HttpStatusCode.BadRequest;
                body = new { message = appException.Message, traceId };
                break;

            case ValidationException validation:
                status = HttpStatusCode.BadRequest;
                body = new
                {
                    message = validation.Errors.FirstOrDefault()?.ErrorMessage ?? "Uppgifterna är ogiltiga.",
                    errors = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
                    traceId
                };
                break;

            default:
                // Full detail goes to the server log, tied to the trace id the user can quote.
                _logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", traceId);
                status = HttpStatusCode.InternalServerError;
                body = _environment.IsDevelopment()
                    ? new
                    {
                        message = GenericErrorMessage,
                        traceId,
                        error = exception.Message,
                        details = exception.InnerException?.Message,
                        stackTrace = exception.StackTrace
                    }
                    : new { message = GenericErrorMessage, traceId };
                break;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
