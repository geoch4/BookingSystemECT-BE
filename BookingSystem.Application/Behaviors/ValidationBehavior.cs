// Backend purpose: Defines a cross-cutting MediatR pipeline behavior for ValidationBehavior.
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FluentValidation;
namespace BookingSystem.Application.Behaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public  async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Run every validator registered for this request type before its handler executes.
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            
            var validationResult = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            
            var failures = validationResult.
                SelectMany(r  => r.Errors)
                .Where(f => f != null)
                .ToList();
            
            if (failures.Any())
                throw new ValidationException(failures);
                
        }

        // Continue to the next pipeline behavior or request handler after successful validation.
        return await next(cancellationToken);

    }
}
