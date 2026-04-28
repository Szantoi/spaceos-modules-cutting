using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Ardalis.Result;

namespace SpaceOS.Modules.Cutting.Application.Adapters;

/// <summary>
/// SEC-06: Detects hardcoded secrets and high-entropy values in JSON configuration blobs.
/// Allows secret references of the form <c>${secret:name}</c>.
/// </summary>
public sealed class ConfigSecretDetector : IConfigSecretDetector
{
    private static readonly Regex SecretKeyPattern = new(
        @"(?i)(api[_-]?key|password|secret|token|credential|client[_-]?secret|private[_-]?key)",
        RegexOptions.Compiled);

    private static readonly Regex SecretRefPattern = new(
        @"^\$\{secret:[A-Za-z0-9_-]{1,100}\}$",
        RegexOptions.Compiled);

    private const double EntropyThreshold = 4.5;

    /// <inheritdoc />
    public Result ValidateConfigJson(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
            return Result.Success();

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(configJson);
        }
        catch (JsonException ex)
        {
            return Result.Invalid(new ValidationError($"ConfigJson is not valid JSON: {ex.Message}"));
        }

        if (root is null)
            return Result.Success();

        var violations = new List<string>();
        WalkNode(root, string.Empty, violations);

        return violations.Count == 0
            ? Result.Success()
            : Result.Invalid(violations.Select(v => new ValidationError(v)).ToList());
    }

    private static void WalkNode(JsonNode node, string path, List<string> violations)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var kv in obj)
                {
                    var childPath = string.IsNullOrEmpty(path) ? kv.Key : $"{path}.{kv.Key}";
                    if (kv.Value is JsonValue stringValue && stringValue.TryGetValue<string>(out var strVal))
                    {
                        if (SecretKeyPattern.IsMatch(kv.Key))
                            CheckValueForSecret(strVal, childPath, violations);
                    }
                    if (kv.Value is not null)
                        WalkNode(kv.Value, childPath, violations);
                }
                break;

            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    if (arr[i] is not null)
                        WalkNode(arr[i]!, $"{path}[{i}]", violations);
                }
                break;
        }
    }

    private static void CheckValueForSecret(string? value, string path, List<string> violations)
    {
        if (string.IsNullOrEmpty(value))
            return;

        // Allow ${secret:name} references
        if (SecretRefPattern.IsMatch(value))
            return;

        // Flag high-entropy strings that look like actual secrets
        var entropy = ShannonEntropy(value);
        if (entropy > EntropyThreshold)
            violations.Add($"Path '{path}' appears to contain a hardcoded secret (entropy {entropy:F2} > {EntropyThreshold}). Use ${{secret:name}} references instead.");
        else
            violations.Add($"Path '{path}' has a secret-like key name. Use ${{secret:name}} references instead of plain-text values.");
    }

    private static double ShannonEntropy(string s)
    {
        if (s.Length == 0)
            return 0;

        var freq = new Dictionary<char, int>();
        foreach (var c in s)
        {
            freq.TryGetValue(c, out var count);
            freq[c] = count + 1;
        }

        var entropy = 0.0;
        foreach (var f in freq.Values)
        {
            var p = (double)f / s.Length;
            entropy -= p * Math.Log2(p);
        }

        return entropy;
    }
}
