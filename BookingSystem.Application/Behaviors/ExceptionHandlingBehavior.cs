// Backend purpose: Defines a cross-cutting MediatR pipeline behavior for ExceptionHandlingBehavior.
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
namespace BookingSystem.Application.Behaviors;

public class ExceptionHandlingBehavior < TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            // Record request context here, then preserve the original exception for API middleware.
            Console.WriteLine($"[EXCEPTION] {typeof(TRequest).Name}: {ex.Message}");
            throw; // Rethrow so the API middleware can create the HTTP error response.
        }
    }
}
