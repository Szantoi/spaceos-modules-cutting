using System.Text;
using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Providers;

public class CutRiteAdapterTests
{
    private readonly Mock<IExternalAdapterTransport> _transportMock = new();
    private readonly Mock<IAdapterCallAuditWriter> _auditMock = new();
    private readonly Mock<ICuttingTenantAccessor> _tenantAccessorMock = new();
    private readonly CutRiteFormatConverter _converter = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private CutRiteAdapter BuildSut()
    {
        _tenantAccessorMock.Setup(t => t.TenantId).Returns(_tenantId);
        return new CutRiteAdapter(
            _transportMock.Object,
            _converter,
            _auditMock.Object,
            _tenantAccessorMock.Object,
            NullLogger<CutRiteAdapter>.Instance);
    }

    private static CuttingSheetDto MakeSheet() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { new CuttingLineDto("Part1", "Panel", 600, 400, 18, 1, false, null) },
            "MDF18", DateTime.UtcNow);

    [Fact]
    public async Task SubmitCuttingSheetAsync_TransportSucceeds_RecordsAuditAndReturnsId()
    {
        var sut = BuildSut();
        var sheet = MakeSheet();
        var correlationId = "corr-1";
        var resultSheetId = Guid.NewGuid();

        _transportMock
            .Setup(t => t.SubmitAsync(It.IsAny<AdapterPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TransportSubmitResult>.Success(
                new TransportSubmitResult(resultSheetId, correlationId, DateTimeOffset.UtcNow)));

        var id = await sut.SubmitCuttingSheetAsync(sheet);

        id.Should().Be(resultSheetId);
        _auditMock.Verify(a => a.RecordSubmitStartedAsync(It.IsAny<Guid>(), "cutrite",
            nameof(sut.SubmitCuttingSheetAsync), _tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitCuttingSheetAsync_TransportFails_RecordsFailureAndThrows()
    {
        var sut = BuildSut();
        var sheet = MakeSheet();

        _transportMock
            .Setup(t => t.SubmitAsync(It.IsAny<AdapterPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TransportSubmitResult>.Error("cli not found"));

        var act = async () => await sut.SubmitCuttingSheetAsync(sheet);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cli not found*");

        _auditMock.Verify(a => a.RecordFailureAsync(It.IsAny<Guid>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNestingResultAsync_PollNotReady_ReturnsEmptyAssignment()
    {
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();

        _transportMock
            .Setup(t => t.PollResultAsync(sheetId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterPayload>.NotFound());

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNestingResultAsync_ValidCsvResult_ReturnsParsedPlacements()
    {
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();
        var csv = "Name,X,Y,Width,Height,Rotated\nPart1,0,0,600,400,false\n";
        var payload = new AdapterPayload("text/csv", Encoding.UTF8.GetBytes(csv), new Dictionary<string, string>());

        _transportMock
            .Setup(t => t.PollResultAsync(sheetId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterPayload>.Success(payload));

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetExecutionStatusAsync_ReturnsNotSupported()
    {
        var sut = BuildSut();
        var result = await sut.GetExecutionStatusAsync(Guid.NewGuid());

        result.Status.Should().Be("NotSupported");
    }

    [Fact]
    public async Task GetWasteReportAsync_ReturnsZeroReport()
    {
        var sut = BuildSut();
        var range = new SpaceOS.Modules.Inventory.Contracts.Dtos.DateRange(
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var result = await sut.GetWasteReportAsync(range);

        result.TotalAreaCut.Should().Be(0m);
    }
}
