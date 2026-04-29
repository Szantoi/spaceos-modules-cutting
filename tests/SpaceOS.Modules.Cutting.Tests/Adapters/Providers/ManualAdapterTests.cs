using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Providers;

public class ManualAdapterTests
{
    private ManualAdapter BuildSut(Mock<IMediator>? mediatorMock = null)
    {
        var mock = mediatorMock ?? new Mock<IMediator>();
        var tenantMock = new Mock<ICuttingTenantAccessor>();
        tenantMock.Setup(t => t.TenantId).Returns(Guid.NewGuid());
        var inner = new CuttingProviderAdapter(mock.Object, tenantMock.Object);
        var builtin = new BuiltinCuttingProvider(inner);
        return new ManualAdapter(builtin);
    }

    [Fact]
    public void Constructor_NullBuiltin_ThrowsArgumentNullException()
    {
        var act = () => new ManualAdapter(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("builtin");
    }

    [Fact]
    public async Task GetNestingResultAsync_AlwaysReturnsEmptyPlacements()
    {
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().BeEmpty();
        result.PanelsRequired.Should().Be(0);
    }

    [Fact]
    public async Task GetExecutionStatusAsync_ReturnsManualMode()
    {
        var sut = BuildSut();
        var sheetId = Guid.NewGuid();

        var result = await sut.GetExecutionStatusAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Status.Should().Be("ManualMode");
    }

    [Fact]
    public async Task GetWasteReportAsync_ReturnsZeroReport()
    {
        var sut = BuildSut();
        var range = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        var result = await sut.GetWasteReportAsync(range);

        result.TotalAreaCut.Should().Be(0m);
        result.TotalWasteArea.Should().Be(0m);
        result.Lines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNestingResultAsync_WastePercentageIsAlwaysZero()
    {
        var sut = BuildSut();
        var result = await sut.GetNestingResultAsync(Guid.NewGuid());

        result.WastePercentage.Should().Be(0m);
    }

    [Fact]
    public async Task GetExecutionStatusAsync_ErrorMessageIsNull()
    {
        var sut = BuildSut();
        var result = await sut.GetExecutionStatusAsync(Guid.NewGuid());

        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SubmitCuttingSheetAsync_ForwardsThrownExceptionFromBuiltin()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<IRequest<Ardalis.Result.Result<Guid>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("no connection"));

        var sut = BuildSut(mediatorMock);

        var sheet = new CuttingSheetDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Array.Empty<CuttingLineDto>(), "MDF18", DateTime.UtcNow);

        var act = async () => await sut.SubmitCuttingSheetAsync(sheet);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
