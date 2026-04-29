using System.Text;
using FluentAssertions;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Providers;

public class CutRiteFormatConverterTests
{
    private readonly CutRiteFormatConverter _sut = new();

    private static CuttingSheetDto MakeSheet(params CuttingLineDto[] lines) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            lines.Length > 0 ? lines : Array.Empty<CuttingLineDto>(),
            "MDF18", DateTime.UtcNow);

    private static CuttingLineDto MakeLine(string name, decimal w = 600, decimal h = 400, int qty = 1) =>
        new(name, "Panel", w, h, 18m, qty, false, null);

    // ── ToVendorInput ────────────────────────────────────────────────────────

    [Fact]
    public void ToVendorInput_ValidSheet_ProducesCsvWithHeader()
    {
        var sheet = MakeSheet(MakeLine("Door-A"));
        var payload = _sut.ToVendorInput(sheet);

        payload.ContentType.Should().Be("text/csv");
        var csv = Encoding.UTF8.GetString(payload.Content);
        csv.Should().StartWith("Name,Width,Height,Quantity");
    }

    [Fact]
    public void ToVendorInput_MultipleLines_ContainsAllPartsAsRows()
    {
        var sheet = MakeSheet(MakeLine("Door-A", 600, 400, 2), MakeLine("Shelf", 800, 200, 5));
        var payload = _sut.ToVendorInput(sheet);

        var csv = Encoding.UTF8.GetString(payload.Content);
        csv.Should().Contain("Door-A,600,400,2");
        csv.Should().Contain("Shelf,800,200,5");
    }

    [Fact]
    public void ToVendorInput_PartNameWithComma_WrapsInQuotes()
    {
        var sheet = MakeSheet(MakeLine("Part, A"));
        var payload = _sut.ToVendorInput(sheet);

        var csv = Encoding.UTF8.GetString(payload.Content);
        csv.Should().Contain("\"Part, A\"");
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
    public void ParseVendorOutput_ValidCsvWithHeader_ReturnsPlacements()
    {
        var sheetId = Guid.NewGuid();
        var csv = "Name,X,Y,Width,Height,Rotated\nDoor-A,10,20,600,400,false\nDoor-B,620,20,300,200,true\n";
        var payload = new AdapterPayload("text/csv", Encoding.UTF8.GetBytes(csv), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.SheetId.Should().Be(sheetId);
        result.Value.Placements.Should().HaveCount(2);
        result.Value.Placements[0].PartName.Should().Be("Door-A");
        result.Value.Placements[0].X.Should().Be(10m);
        result.Value.Placements[1].IsRotated.Should().BeTrue();
    }

    [Fact]
    public void ParseVendorOutput_CsvWithNoHeader_ParsesFromFirstRow()
    {
        var sheetId = Guid.NewGuid();
        var csv = "Part1,0,0,600,400,false\n";
        var payload = new AdapterPayload("text/csv", Encoding.UTF8.GetBytes(csv), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Placements.Should().HaveCount(1);
    }

    [Fact]
    public void ParseVendorOutput_EmptyCsv_ReturnsEmptyPlacements()
    {
        var sheetId = Guid.NewGuid();
        var payload = new AdapterPayload("text/csv", Encoding.UTF8.GetBytes(string.Empty), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Placements.Should().BeEmpty();
    }

    [Fact]
    public void ParseVendorOutput_RowWithFewerThanSixColumns_SkipsRow()
    {
        var sheetId = Guid.NewGuid();
        var csv = "Name,X,Y,Width,Height,Rotated\nPartial,10,20,600\n";
        var payload = new AdapterPayload("text/csv", Encoding.UTF8.GetBytes(csv), new Dictionary<string, string>());

        var result = _sut.ParseVendorOutput(payload, sheetId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Placements.Should().BeEmpty();
    }
}
