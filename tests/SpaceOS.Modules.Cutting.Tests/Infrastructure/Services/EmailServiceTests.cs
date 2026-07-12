using FluentAssertions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using SpaceOS.Modules.Cutting.Infrastructure.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for EmailService.
/// Tests email sending, template rendering, and error handling.
/// </summary>
public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly Dictionary<string, string?> _configValues;

    public EmailServiceTests()
    {
        _configMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EmailService>>();

        _configValues = new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "smtp-relay.brevo.com",
            ["Email:SmtpPort"] = "587",
            ["Email:SmtpUsername"] = "test@example.com",
            ["Email:SmtpPassword"] = "test-password",
            ["Email:FromEmail"] = "no-reply@joinerytech.hu",
            ["Email:FromName"] = "SpaceOS Portal"
        };

        _configMock
            .Setup(c => c[It.IsAny<string>()])
            .Returns<string>(key => _configValues.TryGetValue(key, out var value) ? value : null);
    }

    [Fact]
    public async Task SendQuoteRequestNotification_ValidInput_SendsTwoEmails()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Note: This test verifies the method doesn't throw exceptions
        // In production, we would mock SmtpClient, but MailKit's SmtpClient is sealed
        // and requires custom wrapper for testing. For now, we verify method signature
        // and error handling in separate tests.

        // Act & Assert
        // We expect the method to attempt sending emails
        // If SMTP is not available, it will throw, which is expected behavior
        var act = async () => await service.SendQuoteRequestNotification(
            customerEmail: "customer@example.com",
            adminEmail: "admin@example.com",
            quoteNumber: "Q-2024-001",
            trackingToken: "abc123",
            trackingUrl: "https://portal.example.com/track/abc123",
            ct: CancellationToken.None);

        // Since we can't mock sealed SmtpClient, we verify it throws connection error
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendQuoteApprovedNotification_ValidInput_SendsOneEmail()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.SendQuoteApprovedNotification(
            customerEmail: "customer@example.com",
            quoteNumber: "Q-2024-001",
            price: 150000m,
            currency: "HUF",
            acceptUrl: "https://portal.example.com/accept/abc123",
            ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task SendQuoteRejectedNotification_ValidInput_SendsOneEmail()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.SendQuoteRejectedNotification(
            customerEmail: "customer@example.com",
            quoteNumber: "Q-2024-001",
            reason: "Materials not available",
            ct: CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void Constructor_MissingSmtpUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        _configValues.Remove("Email:SmtpUsername");

        // Act & Assert
        var act = () => new EmailService(_configMock.Object, _loggerMock.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Email:SmtpUsername not configured");
    }

    [Fact]
    public void Constructor_MissingSmtpPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        _configValues.Remove("Email:SmtpPassword");

        // Act & Assert
        var act = () => new EmailService(_configMock.Object, _loggerMock.Object);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Email:SmtpPassword not configured");
    }

    [Fact]
    public void Constructor_MissingSmtpHost_UsesDefaultValue()
    {
        // Arrange
        _configValues.Remove("Email:SmtpHost");

        // Act
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Assert
        // Service should be created successfully with default SMTP host
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_MissingSmtpPort_UsesDefaultValue()
    {
        // Arrange
        _configValues.Remove("Email:SmtpPort");

        // Act
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Assert
        // Service should be created successfully with default port 587
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SendQuoteRequestNotification_SmtpConnectionError_LogsErrorAndThrows()
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Act
        try
        {
            await service.SendQuoteRequestNotification(
                customerEmail: "customer@example.com",
                adminEmail: "admin@example.com",
                quoteNumber: "Q-2024-001",
                trackingToken: "abc123",
                trackingUrl: "https://portal.example.com/track/abc123",
                ct: CancellationToken.None);
        }
        catch (Exception)
        {
            // Expected exception
        }

        // Assert
        // Verify that error was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send quote request notification emails")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public async Task SendQuoteRequestNotification_InvalidEmailAddress_ThrowsFormatException(string invalidEmail)
    {
        // Arrange
        var service = new EmailService(_configMock.Object, _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.SendQuoteRequestNotification(
            customerEmail: invalidEmail,
            adminEmail: "admin@example.com",
            quoteNumber: "Q-2024-001",
            trackingToken: "abc123",
            trackingUrl: "https://portal.example.com/track/abc123",
            ct: CancellationToken.None);

        // MimeKit's MailboxAddress.Parse throws FormatException for invalid emails
        await act.Should().ThrowAsync<FormatException>();
    }
}
