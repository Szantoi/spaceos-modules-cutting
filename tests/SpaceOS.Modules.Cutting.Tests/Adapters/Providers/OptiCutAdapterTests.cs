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

public class OptiCutAdapterTests
{
    private readonly Mock<IExternalAdapterTransport> _transportMock = new();
    private readonly Mock<IAdapterCallAuditWriter> _auditMock = new();
    private readonly Mock<ICuttingTenantAccessor> _tenantAccessorMock = new();
    private readonly OptiCutFormatConverter _converter = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private OptiCutAdapter BuildSut()
    {
        _tenantAccessorMock.Setup(t => t.TenantId).Returns(_tenantId);
        return new OptiCutAdapter(
            _transportMock.Object,
            _converter,
            _auditMock.Object,
            _tenantAccessorMock.Object,
            NullLogger<OptiCutAdapter>.Instance);
    }

    private static CuttingSheetDto MakeSheet() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            new[] { new CuttingLineDto("Part1", "Panel", 600, 400, 18, 1, false, null) },
            "MDF18", DateTime.UtcNow);

    [Fact]
    public async Task SubmitCuttingSheetAsync_TransportSucceeds_RecordsStartAndCompletion()
    {
        var sut = BuildSut();
        var sheet = MakeSheet();
        var correlationId = Guid.NewGuid().ToString();
        var resultSheetId = Guid.NewGuid();

        _transportMock
            .Setup(t => t.SubmitAsync(It.IsAny<AdapterPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TransportSubmitResult>.Success(
                new TransportSubmitResult(resultSheetId, correlationId, DateTimeOffset.UtcNow)));

        var returnedId = await sut.SubmitCuttingSheetAsync(sheet);

        returnedId.Should().Be(resultSheetId);
        _auditMock.Verify(a => a.RecordSubmitStartedAsync(It.IsAny<Guid>(), "opticut",
            nameof(sut.SubmitCuttingSheetAsync), _tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _auditMock.Verify(a => a.RecordSubmitCompletedAsync(It.IsAny<Guid>(), correlationId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitCuttingSheetAsync_TransportFails_RecordsFailureAndThrows()
    {
        var sut = BuildSut();
        var sheet = MakeSheet();

        _transportMock
            .Setup(t => t.SubmitAsync(It.IsAny<AdapterPayload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TransportSubmitResult>.Error("transport unreachable"));

        var act = async () => await sut.SubmitCuttingSheetAsync(sheet);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*transport unreachable*");

        _auditMock.Verify(a => a.RecordFailureAsync(It.IsAny<Guid>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitCuttingSheetAsync_TransportThrows_RecordsExceptionAndRethrows()
    {
        var sut = BuildSut();
        var sheet = MakeSheet();

        _transportMock
            .Setup(t => t.SubmitAsync(It.IsAny<AdapterPayload>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("disk error"));

        await Assert.ThrowsAsync<IOException>(() => sut.SubmitCuttingSheetAsync(sheet));

        _auditMock.Verify(a => a.RecordExceptionAsync(It.IsAny<Guid>(),
            It.IsAny<IOException>(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task GetNestingResultAsync_ValidXmlResult_ReturnsParsedAssignment()
    {
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();
        var xml = "<Result WastePercentage=\"5\" PanelsRequired=\"1\"><Placement Name=\"Part1\" X=\"0\" Y=\"0\" Width=\"600\" Height=\"400\" Rotated=\"false\"/></Result>";
        var payload = new AdapterPayload("application/xml", Encoding.UTF8.GetBytes(xml), new Dictionary<string, string>());

        _transportMock
            .Setup(t => t.PollResultAsync(sheetId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterPayload>.Success(payload));

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().HaveCount(1);
        result.PanelsRequired.Should().Be(1);
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
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNestingResultAsync_InvalidXmlResult_ReturnsEmptyAssignment()
    {
        // When parsed result is malformed, adapter falls back to empty assignment
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();
        var badXml = "<Result><Unclosed";
        var payload = new AdapterPayload("application/xml",
            Encoding.UTF8.GetBytes(badXml), new Dictionary<string, string>());

        _transportMock
            .Setup(t => t.PollResultAsync(sheetId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AdapterPayload>.Success(payload));

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullTransport_ThrowsArgumentNullException()
    {
        _tenantAccessorMock.Setup(t => t.TenantId).Returns(_tenantId);
        var act = () => new OptiCutAdapter(null!, _converter, _auditMock.Object,
            _tenantAccessorMock.Object, NullLogger<OptiCutAdapter>.Instance);
        act.Should().Throw<ArgumentNullException>().WithParameterName("transport");
    }
}
