// Backend purpose: Defines a cross-cutting MediatR pipeline behavior for LoggingBehavior.
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace BookingSystem.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Mark the start and completion of each MediatR request during local diagnostics.
        Console.WriteLine($"[START] {requestName}");
        
        var response = await next();
        
        Console.WriteLine($"[END] {requestName}");
        return response;
    }
    
}
