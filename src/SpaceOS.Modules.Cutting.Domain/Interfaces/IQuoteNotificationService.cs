using SpaceOS.Modules.Cutting.Domain.Aggregates;

namespace SpaceOS.Modules.Cutting.Domain.Interfaces;

/// <summary>
/// Service for sending quote-related email notifications.
/// </summary>
public interface IQuoteNotificationService
{
    /// <summary>
    /// Sends confirmation email to customer after quote request submission.
    /// </summary>
    Task SendQuoteReceivedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct);

    /// <summary>
    /// Sends email to customer when quote is approved with price.
    /// </summary>
    Task SendQuoteApprovedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct);

    /// <summary>
    /// Sends email to customer when quote is rejected.
    /// </summary>
    Task SendQuoteRejectedEmailAsync(CuttingQuoteRequest quote, CancellationToken ct);
}
