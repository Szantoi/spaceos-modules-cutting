using Ardalis.Result;
using FluentValidation;
using MediatR;
using System.Reflection;

namespace SpaceOS.Modules.Cutting.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that validates requests using FluentValidation.
/// For handlers that return Ardalis.Result or Result&lt;T&gt;, validation failures
/// will return Result.Invalid(). For other response types, throws ValidationException.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errors = failures.Select(f => new ValidationError
            {
                Identifier = f.PropertyName,
                ErrorMessage = f.ErrorMessage
            }).ToList();

            // Try to return Result.Invalid for Ardalis.Result types
            var responseType = typeof(TResponse);

            // Check if TResponse is Result or Result<T>
            if (responseType == typeof(Result) ||
                (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>)))
            {
                // Create Result.Invalid() or Result<T>.Invalid()
                var invalidMethod = responseType.IsGenericType
                    ? typeof(Result<>).MakeGenericType(responseType.GetGenericArguments())
                        .GetMethod("Invalid", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ValidationError[]) }, null)
                    : typeof(Result).GetMethod("Invalid", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ValidationError[]) }, null);

                if (invalidMethod != null)
                {
                    var result = invalidMethod.Invoke(null, new object[] { errors.ToArray() });
                    return (TResponse)result!;
                }
            }

            // Fallback: throw ValidationException
            throw new ValidationException(failures);
        }

        return await next().ConfigureAwait(false);
    }
}
