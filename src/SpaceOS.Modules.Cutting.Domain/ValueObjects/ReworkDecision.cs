using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Domain.ValueObjects;

/// <summary>Result of an <see cref="Interfaces.IReworkPolicy"/> evaluation for a single job.</summary>
public sealed record ReworkDecision(
    bool CanReschedule,
    DaySlot? TargetSlot,
    string Reason);
