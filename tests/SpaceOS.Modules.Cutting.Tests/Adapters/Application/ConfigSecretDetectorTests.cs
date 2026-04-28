using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application;

public class ConfigSecretDetectorTests
{
    private readonly ConfigSecretDetector _detector = new();

    [Fact]
    public void ValidateConfigJson_Null_ReturnsSuccess()
    {
        _detector.ValidateConfigJson(null).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_Empty_ReturnsSuccess()
    {
        _detector.ValidateConfigJson(string.Empty).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_ValidNonSecretConfig_ReturnsSuccess()
    {
        var json = """{"endpoint": "https://example.com", "timeout": 30}""";
        _detector.ValidateConfigJson(json).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_ApiKeyWithPlainText_ReturnsInvalid()
    {
        // High-entropy secret-like value
        var json = """{"api_key": "sk-abcdefghijklmnopqrstuvwxyz1234567890ABCDEF"}""";
        var result = _detector.ValidateConfigJson(json);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidateConfigJson_ApiKeyWithSecretRef_ReturnsSuccess()
    {
        var json = """{"api_key": "${secret:my-api-key}"}""";
        _detector.ValidateConfigJson(json).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_PasswordWithSecretRef_ReturnsSuccess()
    {
        var json = """{"password": "${secret:db-password}"}""";
        _detector.ValidateConfigJson(json).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_PasswordLowEntropy_ReturnsInvalid()
    {
        // Short simple string — flagged because key name is "password"
        var json = """{"password": "simple-text"}""";
        var result = _detector.ValidateConfigJson(json);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateConfigJson_SecretKeyHighEntropy_ReturnsInvalid()
    {
        // High-entropy value on any secret-like key name
        var json = """{"client_secret": "xK9mP2qR7vL4nJ8wZ1oU5tY6bN3cX0dA"}""";
        var result = _detector.ValidateConfigJson(json);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateConfigJson_NestedSecretKey_Detected()
    {
        var json = """{"outer": {"inner": {"token": "Bearer xK9mP2qR7vL4nJ8wZ1oU5tY6bN"}}}""";
        var result = _detector.ValidateConfigJson(json);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("token"));
    }

    [Fact]
    public void ValidateConfigJson_InvalidJson_ReturnsInvalid()
    {
        var result = _detector.ValidateConfigJson("{not valid json");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateConfigJson_SecretRefValidPattern_AllowsVariousNames()
    {
        var json = """{"api_key": "${secret:prod-api-key-v2}", "token": "${secret:auth_token_1}"}""";
        _detector.ValidateConfigJson(json).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_WhitespaceOnly_ReturnsSuccess()
    {
        _detector.ValidateConfigJson("   ").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_EmptyObject_ReturnsSuccess()
    {
        _detector.ValidateConfigJson("{}").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateConfigJson_NullJsonValue_ReturnsSuccess()
    {
        // null JSON value on a secret-like key is fine
        var json = """{"api_key": null}""";
        _detector.ValidateConfigJson(json).IsSuccess.Should().BeTrue();
    }
}
