using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Ardalis.Result;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Format;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;

/// <summary>
/// SEC-02: XXE-hardened format converter for the OptiCut file-based XML cutting optimizer.
/// DTD processing is prohibited and entity expansion is disabled.
/// </summary>
internal sealed class OptiCutFormatConverter : IAdapterFormatConverter
{
    /// <inheritdoc />
    public string AdapterName => "opticut";

    private static readonly XmlReaderSettings HardenedSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        MaxCharactersInDocument = 10 * 1024 * 1024,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        CloseInput = true,
    };

    /// <summary>Converts a <see cref="CuttingSheetDto"/> to an OptiCut XML payload.</summary>
    public AdapterPayload ToVendorInput(CuttingSheetDto sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var sb = new StringBuilder();
        sb.AppendLine("<Cutting>");
        sb.AppendLine("  <Parts>");
        foreach (var line in sheet.Lines)
        {
            var escapedName = SecurityElement.Escape(line.Name) ?? string.Empty;
            sb.AppendLine(
                $"    <Part Name=\"{escapedName}\" Width=\"{line.RawWidth}\" Height=\"{line.RawHeight}\" Quantity=\"{line.Quantity}\"/>");
        }

        sb.AppendLine("  </Parts>");
        sb.AppendLine("</Cutting>");

        return new AdapterPayload(
            "application/xml",
            Encoding.UTF8.GetBytes(sb.ToString()),
            new Dictionary<string, string> { ["sheetId"] = sheet.Id.ToString() });
    }

    /// <summary>Parses an OptiCut XML result payload into a <see cref="PanelAssignmentDto"/>.</summary>
    public Result<PanelAssignmentDto> ParseVendorOutput(AdapterPayload payload, Guid sheetId)
    {
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            using var ms = new MemoryStream(payload.Content);
            using var reader = XmlReader.Create(ms, HardenedSettings);
            var doc = XDocument.Load(reader);

            var root = doc.Root;
            if (root is null)
                return Result<PanelAssignmentDto>.Error("XML result has no root element.");

            var placements = root
                .Descendants("Placement")
                .Select(p => new PanelPlacementDto(
                    p.Attribute("Name")?.Value ?? string.Empty,
                    decimal.TryParse(p.Attribute("X")?.Value, out var x) ? x : 0m,
                    decimal.TryParse(p.Attribute("Y")?.Value, out var y) ? y : 0m,
                    decimal.TryParse(p.Attribute("Width")?.Value, out var w) ? w : 0m,
                    decimal.TryParse(p.Attribute("Height")?.Value, out var h) ? h : 0m,
                    string.Equals(p.Attribute("Rotated")?.Value, "true", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var wasteAttr = root.Attribute("WastePercentage")?.Value;
            var waste = decimal.TryParse(wasteAttr, out var w2) ? w2 : 0m;

            var panelsAttr = root.Attribute("PanelsRequired")?.Value;
            var panels = int.TryParse(panelsAttr, out var p2) ? p2 : 0;

            return Result<PanelAssignmentDto>.Success(
                new PanelAssignmentDto(sheetId, placements, waste, panels));
        }
        catch (XmlException ex)
        {
            return Result<PanelAssignmentDto>.Error($"XML parse failed: {ex.Message}");
        }
    }

    // IAdapterFormatConverter untyped bridge — not used by OptiCutAdapter directly
    AdapterPayload IAdapterFormatConverter.ToVendorInput(object request)
    {
        if (request is CuttingSheetDto sheet)
            return ToVendorInput(sheet);
        throw new ArgumentException($"Expected {nameof(CuttingSheetDto)}, got {request?.GetType().Name}.", nameof(request));
    }

    Result<object> IAdapterFormatConverter.ParseVendorOutput(AdapterPayload payload)
        => throw new NotSupportedException("Use the strongly-typed ParseVendorOutput(AdapterPayload, Guid) overload.");
}
