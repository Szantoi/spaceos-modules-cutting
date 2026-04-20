namespace SpaceOS.Modules.Cutting.Application.Strategies;

/// <summary>Result of a planning strategy validation pass.</summary>
public sealed class PlanningValidationResult
{
    private PlanningValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>True when the plan passed all strategy-level validation rules.</summary>
    public bool IsValid { get; }

    /// <summary>Human-readable error messages produced by failed validation rules.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Creates a passing validation result.</summary>
    public static PlanningValidationResult Ok() =>
        new(true, Array.Empty<string>());

    /// <summary>Creates a failing validation result with one or more error messages.</summary>
    public static PlanningValidationResult Fail(params string[] errors) =>
        new(false, errors);
}
