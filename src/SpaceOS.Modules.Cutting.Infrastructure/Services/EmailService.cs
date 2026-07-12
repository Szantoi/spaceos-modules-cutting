using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MailKit.Net.Smtp;
using MimeKit;
using SpaceOS.Modules.Cutting.Application.Services;

namespace SpaceOS.Modules.Cutting.Infrastructure.Services;

/// <summary>
/// Email service implementation using MailKit with Brevo SMTP.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;

        // Read SMTP configuration from appsettings.json
        _smtpHost = _config["Email:SmtpHost"] ?? "smtp-relay.brevo.com";
        _smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
        _smtpUsername = _config["Email:SmtpUsername"] ?? throw new InvalidOperationException("Email:SmtpUsername not configured");
        _smtpPassword = _config["Email:SmtpPassword"] ?? throw new InvalidOperationException("Email:SmtpPassword not configured");
        _fromEmail = _config["Email:FromEmail"] ?? "no-reply@joinerytech.hu";
        _fromName = _config["Email:FromName"] ?? "SpaceOS Portal";
    }

    /// <inheritdoc/>
    public async Task SendQuoteRequestNotification(
        string customerEmail,
        string adminEmail,
        string quoteNumber,
        string trackingToken,
        string trackingUrl,
        CancellationToken ct)
    {
        try
        {
            // Send customer confirmation email
            await SendEmailAsync(
                to: customerEmail,
                subject: $"Quote Request #{quoteNumber} Received",
                htmlBody: RenderCustomerConfirmationTemplate(quoteNumber, trackingToken, trackingUrl),
                ct: ct).ConfigureAwait(false);

            _logger.LogInformation("Customer confirmation email sent to {CustomerEmail} for quote {QuoteNumber}", customerEmail, quoteNumber);

            // Send admin notification email
            await SendEmailAsync(
                to: adminEmail,
                subject: $"New Quote Request #{quoteNumber}",
                htmlBody: RenderAdminNotificationTemplate(quoteNumber, customerEmail, trackingUrl),
                ct: ct).ConfigureAwait(false);

            _logger.LogInformation("Admin notification email sent to {AdminEmail} for quote {QuoteNumber}", adminEmail, quoteNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quote request notification emails for quote {QuoteNumber}", quoteNumber);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendQuoteApprovedNotification(
        string customerEmail,
        string quoteNumber,
        decimal price,
        string currency,
        string acceptUrl,
        CancellationToken ct)
    {
        try
        {
            await SendEmailAsync(
                to: customerEmail,
                subject: $"Quote #{quoteNumber} Approved",
                htmlBody: RenderQuoteApprovedTemplate(quoteNumber, price, currency, acceptUrl),
                ct: ct).ConfigureAwait(false);

            _logger.LogInformation("Quote approved email sent to {CustomerEmail} for quote {QuoteNumber}", customerEmail, quoteNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quote approved notification for quote {QuoteNumber}", quoteNumber);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task SendQuoteRejectedNotification(
        string customerEmail,
        string quoteNumber,
        string? reason,
        CancellationToken ct)
    {
        try
        {
            await SendEmailAsync(
                to: customerEmail,
                subject: $"Quote #{quoteNumber} Update",
                htmlBody: RenderQuoteRejectedTemplate(quoteNumber, reason),
                ct: ct).ConfigureAwait(false);

            _logger.LogInformation("Quote rejected email sent to {CustomerEmail} for quote {QuoteNumber}", customerEmail, quoteNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send quote rejected notification for quote {QuoteNumber}", quoteNumber);
            throw;
        }
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_fromName, _fromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct).ConfigureAwait(false);
        await client.AuthenticateAsync(_smtpUsername, _smtpPassword, ct).ConfigureAwait(false);
        await client.SendAsync(message, ct).ConfigureAwait(false);
        await client.DisconnectAsync(true, ct).ConfigureAwait(false);
    }

    private string RenderCustomerConfirmationTemplate(string quoteNumber, string trackingToken, string trackingUrl)
    {
        // TODO: Load from /EmailTemplates/quote-request-customer.html
        return $"""
            <!DOCTYPE html>
            <html lang="hu">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Quote Request Confirmation</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h1 style="color: #2c5282;">Quote Request Received</h1>
                <p>Thank you for submitting your quote request.</p>
                <p><strong>Quote Number:</strong> {quoteNumber}</p>
                <p><strong>Tracking Token:</strong> {trackingToken}</p>
                <p>You can track your quote status using the link below:</p>
                <p><a href="{trackingUrl}" style="display: inline-block; padding: 10px 20px; background-color: #2c5282; color: white; text-decoration: none; border-radius: 4px;">Track Quote</a></p>
                <p>We will review your request and respond within 24-48 hours.</p>
                <hr style="border: 1px solid #e2e8f0; margin: 20px 0;">
                <p style="font-size: 12px; color: #718096;">This is an automated email. Please do not reply.</p>
            </body>
            </html>
            """;
    }

    private string RenderAdminNotificationTemplate(string quoteNumber, string customerEmail, string trackingUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="hu">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>New Quote Request</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h1 style="color: #c53030;">New Quote Request</h1>
                <p>A new quote request has been submitted.</p>
                <p><strong>Quote Number:</strong> {quoteNumber}</p>
                <p><strong>Customer Email:</strong> {customerEmail}</p>
                <p><a href="{trackingUrl}" style="display: inline-block; padding: 10px 20px; background-color: #c53030; color: white; text-decoration: none; border-radius: 4px;">Review Quote</a></p>
                <hr style="border: 1px solid #e2e8f0; margin: 20px 0;">
                <p style="font-size: 12px; color: #718096;">SpaceOS Customer Portal Notification</p>
            </body>
            </html>
            """;
    }

    private string RenderQuoteApprovedTemplate(string quoteNumber, decimal price, string currency, string acceptUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="hu">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Quote Approved</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h1 style="color: #22543d;">Quote Approved</h1>
                <p>Great news! Your quote has been approved.</p>
                <p><strong>Quote Number:</strong> {quoteNumber}</p>
                <p><strong>Price:</strong> {price:N0} {currency}</p>
                <p>Please review and accept the quote using the link below:</p>
                <p><a href="{acceptUrl}" style="display: inline-block; padding: 10px 20px; background-color: #22543d; color: white; text-decoration: none; border-radius: 4px;">Accept Quote</a></p>
                <p>This quote is valid for 30 days.</p>
                <hr style="border: 1px solid #e2e8f0; margin: 20px 0;">
                <p style="font-size: 12px; color: #718096;">This is an automated email. Please do not reply.</p>
            </body>
            </html>
            """;
    }

    private string RenderQuoteRejectedTemplate(string quoteNumber, string? reason)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="hu">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Quote Update</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <h1 style="color: #742a2a;">Quote Update</h1>
                <p>We're sorry, but we are unable to fulfill your quote request at this time.</p>
                <p><strong>Quote Number:</strong> {quoteNumber}</p>
                {(reason != null ? $"<p><strong>Reason:</strong> {reason}</p>" : "")}
                <p>If you have questions, please contact our customer support.</p>
                <hr style="border: 1px solid #e2e8f0; margin: 20px 0;">
                <p style="font-size: 12px; color: #718096;">This is an automated email. Please do not reply.</p>
            </body>
            </html>
            """;
    }
}
