using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>
/// Inspects a JSON configuration blob for hardcoded secrets or high-entropy values (SEC-06).
/// </summary>
public interface IConfigSecretDetector
{
    /// <summary>
    /// Validates that <paramref name="configJson"/> contains no plaintext secrets.
    /// Returns <see cref="Result.Invalid"/> with a list of violations on failure.
    /// Returns <see cref="Result.Success"/> when no issues are found.
    /// </summary>
    Result ValidateConfigJson(string? configJson);
}
