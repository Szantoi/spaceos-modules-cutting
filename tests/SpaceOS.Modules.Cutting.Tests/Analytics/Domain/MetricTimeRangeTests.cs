using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Domain;

public class MetricTimeRangeTests
{
    private static readonly DateTimeOffset Base = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_FromBeforeTo_ReturnsSuccess()
    {
        var result = MetricTimeRange.Create(Base, Base.AddDays(7), MetricResolution.Daily);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_FromEqualsTo_ReturnsInvalid()
    {
        var result = MetricTimeRange.Create(Base, Base, MetricResolution.Daily);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_FromAfterTo_ReturnsInvalid()
    {
        var result = MetricTimeRange.Create(Base.AddDays(1), Base, MetricResolution.Daily);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_SpanOver365Days_ReturnsInvalid()
    {
        var result = MetricTimeRange.Create(Base, Base.AddDays(366), MetricResolution.Daily);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_HourlyResolution_StoredCorrectly()
    {
        var result = MetricTimeRange.Create(Base, Base.AddHours(24), MetricResolution.Hourly);
        result.IsSuccess.Should().BeTrue();
        result.Value.Resolution.Should().Be(MetricResolution.Hourly);
    }

    [Fact]
    public void Create_DailyResolution_StoredCorrectly()
    {
        var result = MetricTimeRange.Create(Base, Base.AddDays(30), MetricResolution.Daily);
        result.IsSuccess.Should().BeTrue();
        result.Value.Resolution.Should().Be(MetricResolution.Daily);
    }

    [Fact]
    public void Create_Exactly365Days_ReturnsSuccess()
    {
        var result = MetricTimeRange.Create(Base, Base.AddDays(365), MetricResolution.Daily);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_366Days_ReturnsInvalid()
    {
        var result = MetricTimeRange.Create(Base, Base.AddDays(366), MetricResolution.Daily);
        result.IsSuccess.Should().BeFalse();
    }
}
