using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Infrastructure.Services;

/// <summary>
/// Stub implementation of quote notification service.
/// TODO: Integrate with Brevo SMTP (smtp-relay.brevo.com:587).
/// </summary>
public sealed class QuoteNotificationService : IQuoteNotificationService
{
    private readonly ILogger<QuoteNotificationService> _logger;

    public QuoteNotificationService(ILogger<QuoteNotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task SendQuoteReceivedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct)
    {
        _logger.LogInformation(
            "TODO: Send 'Quote Received' email to {Email} for quote {QuoteNumber}",
            quote.CustomerContact.Email,
            quote.QuoteNumber);

        // TODO: Implement Brevo SMTP integration
        // Template: "Ajánlatkérését fogadtuk, 24 órán belül válaszolunk"
        // Include tracking URL: /public/cutting/quotes/track/{trackingToken}

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendQuoteApprovedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct)
    {
        _logger.LogInformation(
            "TODO: Send 'Quote Approved' email to {Email} for quote {QuoteNumber} with price {Price} {Currency}",
            quote.CustomerContact.Email,
            quote.QuoteNumber,
            quote.QuotedPrice?.Amount,
            quote.QuotedPrice?.Currency);

        // TODO: Implement Brevo SMTP integration
        // Template: "Ajánlatunk: {price} HUF, kattintson az elfogadáshoz: {trackingUrl}"
        // Include accept URL: /public/cutting/quotes/track/{trackingToken}/accept

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendQuoteRejectedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct)
    {
        _logger.LogInformation(
            "TODO: Send 'Quote Rejected' email to {Email} for quote {QuoteNumber}. Reason: {Reason}",
            quote.CustomerContact.Email,
            quote.QuoteNumber,
            quote.RejectionReason);

        // TODO: Implement Brevo SMTP integration
        // Template: "Sajnáljuk, a kért munka nem vállalható: {reason}"

        return Task.CompletedTask;
    }
}
