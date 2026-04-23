namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>A single ordering rule within a PriorityProfile.</summary>
public sealed record PriorityRule(
    int Order,
    string RuleName,
    string? Parameter = null);
