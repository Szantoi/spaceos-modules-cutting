using System.Text.RegularExpressions;

namespace SpaceOS.Modules.Cutting.Domain.Adapters;

/// <summary>
/// Sanitizes strings before they are stored in audit or health records.
/// Strips control characters and truncates to a safe maximum length.
/// </summary>
public static class AuditSanitizer
{
    private static readonly Regex ControlChars =
        new(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);

    /// <summary>Maximum number of characters stored in an error field.</summary>
    public const int MaxErrorLength = 8000;

    /// <summary>
    /// Strips ASCII control characters and truncates <paramref name="input"/> to
    /// <see cref="MaxErrorLength"/> characters. Returns an empty string for null input.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var cleaned = ControlChars.Replace(input, string.Empty);
        return cleaned.Length > MaxErrorLength
            ? cleaned[..MaxErrorLength]
            : cleaned;
    }
}
