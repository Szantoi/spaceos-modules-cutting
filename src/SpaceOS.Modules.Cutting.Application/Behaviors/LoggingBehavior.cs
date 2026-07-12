using MediatR;
using Microsoft.Extensions.Logging;

namespace SpaceOS.Modules.Cutting.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request handling and exceptions.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        try
        {
            _logger.LogInformation("Handling {RequestName}", requestName);
            var response = await next().ConfigureAwait(false);
            _logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling {RequestName}: {Message}", requestName, ex.Message);
            throw;
        }
    }
}
