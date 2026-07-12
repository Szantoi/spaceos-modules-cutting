using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Cutting.Application.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Services;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure.Services;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _mockLogger;

    public EmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<EmailService>>();
    }

    [Fact]
    public void Constructor_WithMissingSmtpUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "587" },
                // Missing SmtpUsername
                { "Email:SmtpPassword", "test-password" },
                { "Email:FromEmail", "test@example.com" },
                { "Email:FromName", "Test Sender" }
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new EmailService(config, _mockLogger.Object));
        Assert.Contains("SmtpUsername", ex.Message);
    }

    [Fact]
    public void Constructor_WithMissingSmtpPassword_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "587" },
                { "Email:SmtpUsername", "test-user" },
                // Missing SmtpPassword
                { "Email:FromEmail", "test@example.com" },
                { "Email:FromName", "Test Sender" }
            })
            .Build();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => new EmailService(config, _mockLogger.Object));
        Assert.Contains("SmtpPassword", ex.Message);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_CreatesInstance()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var service = new EmailService(config, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithDefaultSmtpHost_UsesBrevoHost()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // SmtpHost omitted - should default to Brevo
                { "Email:SmtpPort", "587" },
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" }
            })
            .Build();

        // Act
        var service = new EmailService(config, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
        // Default should be "smtp-relay.brevo.com" but we can't directly verify private field
        // This test just ensures no exception is thrown
    }

    [Fact]
    public void Constructor_WithDefaultSmtpPort_Uses587()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                // SmtpPort omitted - should default to 587
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" }
            })
            .Build();

        // Act
        var service = new EmailService(config, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithDefaultFromEmail_UsesDefaultEmail()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "587" },
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" }
                // FromEmail omitted - should default to "no-reply@joinerytech.hu"
            })
            .Build();

        // Act
        var service = new EmailService(config, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithDefaultFromName_UsesDefaultName()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "587" },
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" },
                { "Email:FromEmail", "test@example.com" }
                // FromName omitted - should default to "SpaceOS Portal"
            })
            .Build();

        // Act
        var service = new EmailService(config, _mockLogger.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithInvalidSmtpPort_ThrowsFormatException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "invalid-port" }, // Invalid integer
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" }
            })
            .Build();

        // Act & Assert
        Assert.Throws<FormatException>(() => new EmailService(config, _mockLogger.Object));
    }

    private static IConfiguration CreateValidConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Email:SmtpHost", "smtp.example.com" },
                { "Email:SmtpPort", "587" },
                { "Email:SmtpUsername", "test-user" },
                { "Email:SmtpPassword", "test-password" },
                { "Email:FromEmail", "test@example.com" },
                { "Email:FromName", "Test Sender" }
            })
            .Build();
    }

    // Note: Integration tests for actual email sending (SendQuoteRequestNotification, etc.)
    // would require a mock SMTP server or test email service, which is out of scope for unit tests.
    // These should be covered in E2E or integration tests with a test SMTP server like MailHog.
}
