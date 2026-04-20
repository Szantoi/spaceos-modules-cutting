using FluentAssertions;
using SpaceOS.Modules.Cutting.Application.Strategies;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

public class PlanningStrategyFactoryTests
{
    private readonly PlanningStrategyFactory _sut = new();

    [Fact]
    public void GetStrategy_WithMaxcutV1_ReturnsMaxCutStrategy()
    {
        var strategy = _sut.GetStrategy("maxcut-v1");
        strategy.Should().BeOfType<MaxCutStrategy>();
    }

    [Fact]
    public void GetStrategy_WithFifo_ReturnsFIFOStrategy()
    {
        var strategy = _sut.GetStrategy("fifo");
        strategy.Should().BeOfType<FIFOStrategy>();
    }

    [Fact]
    public void GetStrategy_WithPriority_ReturnsPriorityStrategy()
    {
        var strategy = _sut.GetStrategy("priority");
        strategy.Should().BeOfType<PriorityStrategy>();
    }

    [Fact]
    public void GetStrategy_WithCustom_ReturnsCustomStrategy()
    {
        var strategy = _sut.GetStrategy("custom");
        strategy.Should().BeOfType<CustomStrategy>();
    }

    [Fact]
    public void GetStrategy_WithUnknownId_ThrowsArgumentException()
    {
        var act = () => _sut.GetStrategy("unknown-strategy-xyz");
        act.Should().Throw<ArgumentException>().WithParameterName("strategyId");
    }
}
