using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Cutting.Application.Queries.GetExecutionStatus;
using SpaceOS.Modules.Cutting.Application.Queries.GetNestingResult;
using SpaceOS.Modules.Cutting.Application.Queries.GetWasteReport;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;
using SpaceOS.Modules.Inventory.Contracts.Dtos;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Providers;

/// <summary>
/// Verifies that BuiltinCuttingProvider correctly delegates every ICuttingProvider call
/// to the underlying CuttingProviderAdapter without transforming the return value.
/// </summary>
public class BuiltinCuttingProviderTests
{
    private static (BuiltinCuttingProvider Sut, Mock<IMediator> MediatorMock) Build()
    {
        var mediatorMock = new Mock<IMediator>();
        var tenantAccessorMock = new Mock<ICuttingTenantAccessor>();
        tenantAccessorMock.Setup(t => t.TenantId).Returns(Guid.NewGuid());

        var inner = new CuttingProviderAdapter(mediatorMock.Object, tenantAccessorMock.Object);
        var sut = new BuiltinCuttingProvider(inner);
        return (sut, mediatorMock);
    }

    [Fact]
    public void Constructor_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new BuiltinCuttingProvider(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public async Task GetNestingResultAsync_InnerReturnsNotFound_ReturnsEmptyAssignment()
    {
        var (sut, mediatorMock) = Build();
        var sheetId = Guid.NewGuid();

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetNestingResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<NestingResultResponse>.NotFound());

        var result = await sut.GetNestingResultAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Placements.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExecutionStatusAsync_InnerReturnsNotFound_ReturnsNotFoundStatus()
    {
        var (sut, mediatorMock) = Build();
        var sheetId = Guid.NewGuid();

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetExecutionStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ExecutionStatusResponse>.NotFound());

        var result = await sut.GetExecutionStatusAsync(sheetId);

        result.SheetId.Should().Be(sheetId);
        result.Status.Should().Be("NotFound");
    }

    [Fact]
    public async Task GetWasteReportAsync_InnerReturnsError_ReturnsZeroWasteReport()
    {
        var (sut, mediatorMock) = Build();
        var range = new DateRange(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetWasteReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WasteReportResponse>.Error("no data"));

        var result = await sut.GetWasteReportAsync(range);

        result.TotalAreaCut.Should().Be(0m);
        result.Lines.Should().BeEmpty();
    }
}
