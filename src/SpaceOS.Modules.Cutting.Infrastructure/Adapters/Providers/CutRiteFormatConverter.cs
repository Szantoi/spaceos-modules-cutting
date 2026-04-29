using System.Globalization;
using System.Text;
using Ardalis.Result;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Format;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;

/// <summary>
/// CSV-based format converter for the CutRite CLI cutting optimizer.
/// Input: Name,Width,Height,Quantity CSV rows. Output: parsed result CSV into <see cref="PanelAssignmentDto"/>.
/// </summary>
internal sealed class CutRiteFormatConverter : IAdapterFormatConverter
{
    private const string Header = "Name,Width,Height,Quantity";

    /// <inheritdoc />
    public string AdapterName => "cutrite";

    /// <summary>Converts a <see cref="CuttingSheetDto"/> to a CutRite CSV payload.</summary>
    public AdapterPayload ToVendorInput(CuttingSheetDto sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var sb = new StringBuilder();
        sb.AppendLine(Header);
        foreach (var line in sheet.Lines)
        {
            // Escape name: wrap in quotes if contains comma/quote; double-escape inner quotes
            var escapedName = EscapeCsvField(line.Name);
            sb.AppendLine(
                $"{escapedName},{line.RawWidth.ToString(CultureInfo.InvariantCulture)},{line.RawHeight.ToString(CultureInfo.InvariantCulture)},{line.Quantity}");
        }

        return new AdapterPayload(
            "text/csv",
            Encoding.UTF8.GetBytes(sb.ToString()),
            new Dictionary<string, string> { ["sheetId"] = sheet.Id.ToString() });
    }

    /// <summary>
    /// Parses a CutRite result CSV into a <see cref="PanelAssignmentDto"/>.
    /// Expected result format: Name,X,Y,Width,Height,Rotated
    /// </summary>
    public Result<PanelAssignmentDto> ParseVendorOutput(AdapterPayload payload, Guid sheetId)
    {
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            var csv = Encoding.UTF8.GetString(payload.Content);
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var placements = new List<PanelPlacementDto>();

            // Skip header line if present
            var startIndex = lines.Length > 0 &&
                             lines[0].StartsWith("Name", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            for (var i = startIndex; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var parts = trimmed.Split(',');
                if (parts.Length < 6)
                    continue;

                placements.Add(new PanelPlacementDto(
                    parts[0].Trim('"'),
                    decimal.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var x) ? x : 0m,
                    decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var y) ? y : 0m,
                    decimal.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var w) ? w : 0m,
                    decimal.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var h) ? h : 0m,
                    string.Equals(parts[5].Trim(), "true", StringComparison.OrdinalIgnoreCase)));
            }

            return Result<PanelAssignmentDto>.Success(
                new PanelAssignmentDto(sheetId, placements, 0m, placements.Count > 0 ? 1 : 0));
        }
        catch (Exception ex)
        {
            return Result<PanelAssignmentDto>.Error($"CSV parse failed: {ex.Message}");
        }
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    // IAdapterFormatConverter untyped bridge
    AdapterPayload IAdapterFormatConverter.ToVendorInput(object request)
    {
        if (request is CuttingSheetDto sheet)
            return ToVendorInput(sheet);
        throw new ArgumentException($"Expected {nameof(CuttingSheetDto)}, got {request?.GetType().Name}.", nameof(request));
    }

    Result<object> IAdapterFormatConverter.ParseVendorOutput(AdapterPayload payload)
        => throw new NotSupportedException("Use the strongly-typed ParseVendorOutput(AdapterPayload, Guid) overload.");
}
