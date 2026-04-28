using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Domain;

public class AuditSanitizerTests
{
    [Fact]
    public void Sanitize_NullInput_ReturnsEmptyString()
    {
        AuditSanitizer.Sanitize(null).Should().Be(string.Empty);
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmptyString()
    {
        AuditSanitizer.Sanitize(string.Empty).Should().Be(string.Empty);
    }

    [Fact]
    public void Sanitize_NormalText_ReturnsUnchanged()
    {
        const string input = "Connection timeout after 30 seconds.";
        AuditSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_ControlChars_StripsAll()
    {
        // CR + LF injection attempt
        var input = "Error\r\nInjected line\x00null-byte";
        var result = AuditSanitizer.Sanitize(input);
        result.Should().NotContain("\r");
        result.Should().NotContain("\n");
        result.Should().NotContain("\x00");
        result.Should().Contain("Error");
        result.Should().Contain("Injected line");
    }

    [Fact]
    public void Sanitize_BellCharacter_Stripped()
    {
        // \a is BEL (0x07), within the \x00-\x1F range
        var input = "Error" + '\a' + "beep";
        AuditSanitizer.Sanitize(input).Should().Be("Errorbeep");
    }

    [Fact]
    public void Sanitize_DeleteCharacter_Stripped()
    {
        // DEL is 0x7F; kept separate from the \x00-\x1F block
        var input = "abc" + '\x7F' + "def";
        AuditSanitizer.Sanitize(input).Should().Be("abcdef");
    }

    [Fact]
    public void Sanitize_ExceedsMaxLength_Truncated()
    {
        var input = new string('A', AuditSanitizer.MaxErrorLength + 500);
        var result = AuditSanitizer.Sanitize(input);
        result.Length.Should().Be(AuditSanitizer.MaxErrorLength);
    }

    [Fact]
    public void Sanitize_ExactlyMaxLength_NotTruncated()
    {
        var input = new string('B', AuditSanitizer.MaxErrorLength);
        var result = AuditSanitizer.Sanitize(input);
        result.Length.Should().Be(AuditSanitizer.MaxErrorLength);
    }

    [Fact]
    public void Sanitize_CRLFInjection_StrippedBeforeTruncation()
    {
        // 100 CRLFs followed by normal text — after stripping, it should just be normal text
        var input = new string('\n', 100) + "safe message";
        var result = AuditSanitizer.Sanitize(input);
        result.Should().Be("safe message");
    }

    [Fact]
    public void MaxErrorLength_Is8000()
    {
        AuditSanitizer.MaxErrorLength.Should().Be(8000);
    }
}
