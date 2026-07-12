namespace SpaceOS.Modules.Cutting.Application.Services;

/// <summary>
/// Service for sending transactional emails via SMTP (Brevo).
/// Used by the Customer Portal for quote request notifications.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends notification emails when a new quote request is submitted.
    /// Sends two emails: one to the customer (confirmation) and one to the admin (notification).
    /// </summary>
    /// <param name="customerEmail">Customer's email address.</param>
    /// <param name="adminEmail">Admin email address (manufacturer contact).</param>
    /// <param name="quoteNumber">Quote tracking number.</param>
    /// <param name="trackingToken">Secure tracking token for customer.</param>
    /// <param name="trackingUrl">Full URL for tracking the quote.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendQuoteRequestNotification(
        string customerEmail,
        string adminEmail,
        string quoteNumber,
        string trackingToken,
        string trackingUrl,
        CancellationToken ct);

    /// <summary>
    /// Sends email notification when a quote is approved with pricing.
    /// </summary>
    /// <param name="customerEmail">Customer's email address.</param>
    /// <param name="quoteNumber">Quote tracking number.</param>
    /// <param name="price">Quoted price amount.</param>
    /// <param name="currency">Currency code (e.g., "HUF").</param>
    /// <param name="acceptUrl">URL for customer to accept the quote.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendQuoteApprovedNotification(
        string customerEmail,
        string quoteNumber,
        decimal price,
        string currency,
        string acceptUrl,
        CancellationToken ct);

    /// <summary>
    /// Sends email notification when a quote is rejected.
    /// </summary>
    /// <param name="customerEmail">Customer's email address.</param>
    /// <param name="quoteNumber">Quote tracking number.</param>
    /// <param name="reason">Rejection reason (optional).</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendQuoteRejectedNotification(
        string customerEmail,
        string quoteNumber,
        string? reason,
        CancellationToken ct);
}
