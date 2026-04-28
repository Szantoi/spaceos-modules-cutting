using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Domain;

public class OEEScoreTests
{
    [Fact]
    public void Create_WithValidComponents_ComputesCorrectOverall()
    {
        var result = OEEScore.Create(0.9m, 0.8m, 0.95m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(0.9m * 0.8m * 0.95m);
    }

    [Fact]
    public void Create_WithNegativeAvailability_ReturnsInvalid()
    {
        var result = OEEScore.Create(-0.1m, 0.8m, 0.9m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_WithAvailabilityAboveOne_ReturnsInvalid()
    {
        var result = OEEScore.Create(1.01m, 0.8m, 0.9m);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_AllOnes_OverallIsOne()
    {
        var result = OEEScore.Create(1.0m, 1.0m, 1.0m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(1.0m);
    }

    [Fact]
    public void Create_AnyComponentZero_OverallIsZero()
    {
        var result = OEEScore.Create(0.9m, 0m, 0.95m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(0m);
    }

    [Fact]
    public void Create_AllZeros_OverallIsZero()
    {
        var result = OEEScore.Create(0m, 0m, 0m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(0m);
    }

    [Fact]
    public void Create_SameValues_ProduceSameOverall()
    {
        var r1 = OEEScore.Create(0.7m, 0.8m, 0.9m);
        var r2 = OEEScore.Create(0.7m, 0.8m, 0.9m);
        r1.Value.Overall.Should().Be(r2.Value.Overall);
    }

    [Fact]
    public void Create_AvailabilityOnePerformanceOneQualityHalf_OverallIsHalf()
    {
        var result = OEEScore.Create(1m, 1m, 0.5m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(0.5m);
    }

    [Fact]
    public void Create_FractionalValues_CorrectDecimalMultiplication()
    {
        var result = OEEScore.Create(0.85m, 0.75m, 0.60m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Overall.Should().Be(0.85m * 0.75m * 0.60m);
    }

    [Fact]
    public void Create_BoundaryZeroAvailability_IsValid()
    {
        var result = OEEScore.Create(0.0m, 0.5m, 0.5m);
        result.IsSuccess.Should().BeTrue();
        result.Value.Availability.Should().Be(0.0m);
    }
}
