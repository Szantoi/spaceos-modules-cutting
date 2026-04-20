namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>Resolves a <see cref="IPlanningStrategy"/> by its well-known strategy identifier.</summary>
public interface IPlanningStrategyFactory
{
    /// <summary>
    /// Returns the strategy associated with <paramref name="strategyId"/>.
    /// </summary>
    /// <param name="strategyId">
    /// One of: <c>maxcut-v1</c>, <c>fifo</c>, <c>priority</c>, <c>custom</c>.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="strategyId"/> is not recognised.</exception>
    IPlanningStrategy GetStrategy(string strategyId);
}

/// <inheritdoc cref="IPlanningStrategyFactory"/>
public sealed class PlanningStrategyFactory : IPlanningStrategyFactory
{
    /// <inheritdoc/>
    public IPlanningStrategy GetStrategy(string strategyId) => strategyId switch
    {
        "maxcut-v1" => new MaxCutStrategy(),
        "fifo"      => new FIFOStrategy(),
        "priority"  => new PriorityStrategy(),
        "custom"    => new CustomStrategy(),
        _           => throw new ArgumentException($"Unknown planning strategy: '{strategyId}'.", nameof(strategyId))
    };
}
