using System.Text;
using FluentAssertions;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Providers;

public class OptiCutFormatConverterTests
{
    private readonly OptiCutFormatConverter _sut = new();

    private static CuttingSheetDto MakeSheet(params CuttingLineDto[] lines) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            lines.Length > 0 ? lines : Array.Empty<CuttingLineDto>(),
            "MDF18", DateTime.UtcNow);

    private static CuttingLineDto MakeLine(string name, decimal w = 600, decimal h = 400, int qty = 1) =>
        new(name, "Panel", w, h, 18m, qty, false, null);

    // ── ToVendorInput ────────────────────────────────────────────────────────

    [Fact]
    public void ToVendorInput_ValidSheet_ProducesXmlWithCorrectContentType()
    {
        var sheet = MakeSheet(MakeLine("Door-A"));
        var payload = _sut.ToVendorInput(sheet);

        payload.ContentType.Should().Be("application/xml");
    }

    [Fact]
    public void ToVendorInput_ValidSheet_XmlContainsPartElements()
    {
        var sheet = MakeSheet(MakeLine("Door-A", 600, 400, 2));
        var payload = _sut.ToVendorInput(sheet);

        var xml = Encoding.UTF8.GetString(payload.Content);
        xml.Should().Contain("<Cutting>")
            .And.Contain("<Part")
            .And.Contain("Name=\"Door-A\"")
            .And.Contain("Width=\"600\"")
            .And.Contain("Height=\"400\"")
            .And.Contain("Quantity=\"2\"");
    }

    [Fact]
    public void ToVendorInput_PartNameWithXmlSpecialChars_EscapesCorrectly()
    {
        // SEC-02: names with < > & ' " must be escaped to prevent XML injection
        var sheet = MakeSheet(MakeLine("Part<>&\"'"));
        var payload = _sut.ToVendorInput(sheet);

        var xml = Encoding.UTF8.GetString(payload.Content);
        xml.Should().Contain("Part&lt;&gt;&amp;&quot;&apos;");
        xml.Should().NotContain("Name=\"Part<>");
    }

    [Fact]
    public void ToVendorInput_EmptyLines_ProducesEmptyPartsSection()
    {
        var sheet = MakeSheet();
        var payload = _sut.ToVendorInput(sheet);

        var xml = Encoding.UTF8.GetString(payload.Content);
        xml.Should().Contain("<Parts>").And.Contain("</Parts>");
        xml.Should().NotContain("<Part ");
    }

    [Fact]
    public void ToVendorInput_SheetIdStoredInMetadata()
    {
        var sheet = MakeSheet(MakeLine("Door"));
        var payload = _sut.ToVendorInput(sheet);

        payload.Metadata.Should().ContainKey("sheetId");
        payload.Metadata["sheetId"].Should().Be(sheet.Id.ToString());
    }

    // ── ParseVendorOutput ────────────────────────────────────────────────────

    [Fact]
    public void ParseVendorOutput_ValidResultXml_ReturnsPanelAssignmentDto()
    {
        var sheetId = Guid.NewGuid();
        var xml = """
            <Result WastePercentage="8.5" PanelsRequired="3">
                <Placement Name="Door-A" X="10" Y="20" Width="600" Height="400" Rotated="false"/>
                <Placement Name="Door-B" X="620" Y="20" Width="300" Height="200" Rotated="true"/>
            </Result>
            """;
        var payload = new AdapterPayload("application/xml", Encoding.UTF8.GetBytes(xml), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.SheetId.Should().Be(sheetId);
        result.Value.PanelsRequired.Should().Be(3);
        result.Value.WastePercentage.Should().Be(8.5m);
        result.Value.Placements.Should().HaveCount(2);
        result.Value.Placements[0].PartName.Should().Be("Door-A");
        result.Value.Placements[1].IsRotated.Should().BeTrue();
    }

    [Fact]
    public void ParseVendorOutput_MalformedXml_ReturnsError()
    {
        var sheetId = Guid.NewGuid();
        var payload = new AdapterPayload("application/xml",
            Encoding.UTF8.GetBytes("<Result><Unclosed"), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeFalse();
        string.Join(" ", result.Errors).Should().Contain("XML parse failed");
    }

    [Fact]
    public void ParseVendorOutput_XxePayload_ReturnsError()
    {
        // SEC-02: XXE attack via DOCTYPE/ENTITY must be blocked
        var sheetId = Guid.NewGuid();
        var xxeXml = """
            <?xml version="1.0"?>
            <!DOCTYPE foo [
              <!ENTITY xxe SYSTEM "file:///etc/passwd">
            ]>
            <Result>&xxe;</Result>
            """;
        var payload = new AdapterPayload("application/xml",
            Encoding.UTF8.GetBytes(xxeXml), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseVendorOutput_BillionLaughsPayload_ReturnsError()
    {
        // SEC-02: entity expansion bomb (billion laughs) must be blocked
        var sheetId = Guid.NewGuid();
        var bombXml = """
            <?xml version="1.0"?>
            <!DOCTYPE lolz [
              <!ENTITY lol "lol">
              <!ENTITY lol2 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
              <!ENTITY lol3 "&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;">
            ]>
            <Result>&lol3;</Result>
            """;
        var payload = new AdapterPayload("application/xml",
            Encoding.UTF8.GetBytes(bombXml), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ParseVendorOutput_EmptyPlacements_ReturnsEmptyList()
    {
        var sheetId = Guid.NewGuid();
        var xml = "<Result WastePercentage=\"0\" PanelsRequired=\"0\"></Result>";
        var payload = new AdapterPayload("application/xml",
            Encoding.UTF8.GetBytes(xml), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Placements.Should().BeEmpty();
    }
}
