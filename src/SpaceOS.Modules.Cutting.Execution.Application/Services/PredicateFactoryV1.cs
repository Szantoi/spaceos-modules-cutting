using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Services;

namespace SpaceOS.Modules.Cutting.Execution.Application.Services;

/// <summary>
/// Factory that creates built-in <see cref="IMilestonePredicate"/> instances from a kind + config pair.
/// </summary>
public sealed class PredicateFactoryV1
{
    /// <summary>Creates the predicate for the given milestone kind, applying config JSON where applicable.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="kind"/> is not recognised.</exception>
    public IMilestonePredicate Create(MilestoneKind kind, string configJson, int configVersion)
        => kind switch
        {
            MilestoneKind.PanelCompletion => new PanelCompletionPredicate(),
            MilestoneKind.TimeWindow => new TimeWindowPredicate(),
            MilestoneKind.QualityCheck => BuildQualityCheck(configJson),
            MilestoneKind.WorkerConsent => new WorkerConsentPredicate(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown milestone kind.")
        };

    private static QualityCheckPredicate BuildQualityCheck(string configJson)
    {
        // Simple JSON parse: expects {"maxOffcutRatio": 0.30}
        if (!string.IsNullOrWhiteSpace(configJson))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(configJson);
                if (doc.RootElement.TryGetProperty("maxOffcutRatio", out var prop) &&
                    prop.TryGetDecimal(out var ratio))
                {
                    return new QualityCheckPredicate(ratio);
                }
            }
            catch (System.Text.Json.JsonException) { /* fall through to default */ }
        }
        return new QualityCheckPredicate();
    }
}
