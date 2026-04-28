using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Domain;

public class AnonymizationPolicyTests
{
    [Fact]
    public void Default_HasExpectedValues()
    {
        var policy = AnonymizationPolicy.Default;
        policy.KThreshold.Should().Be(5);
        policy.LDiversityMin.Should().Be(2);
        policy.MinDaysWindow.Should().Be(7);
    }

    [Fact]
    public void Create_KThresholdOne_ReturnsInvalid()
    {
        var result = AnonymizationPolicy.Create(1, 2, 7);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_LDiversityMinZero_ReturnsInvalid()
    {
        var result = AnonymizationPolicy.Create(5, 0, 7);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_MinDaysWindowZero_ReturnsInvalid()
    {
        var result = AnonymizationPolicy.Create(5, 2, 0);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_MinimumValidValues_ReturnsSuccess()
    {
        var result = AnonymizationPolicy.Create(2, 1, 1);
        result.IsSuccess.Should().BeTrue();
        result.Value.KThreshold.Should().Be(2);
    }

    [Fact]
    public void Create_LargeValues_ReturnsSuccess()
    {
        var result = AnonymizationPolicy.Create(10, 5, 30);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Default_IsNotNull()
    {
        AnonymizationPolicy.Default.Should().NotBeNull();
    }

    [Fact]
    public void Properties_AreAccessible()
    {
        var result = AnonymizationPolicy.Create(3, 2, 14);
        result.Value.KThreshold.Should().Be(3);
        result.Value.LDiversityMin.Should().Be(2);
        result.Value.MinDaysWindow.Should().Be(14);
    }
}
