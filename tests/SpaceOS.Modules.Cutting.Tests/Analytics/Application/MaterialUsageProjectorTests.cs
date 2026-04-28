using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Application;

public class MaterialUsageProjectorTests
{
    private readonly Mock<IProjectionIdempotencyGate> _gate = new();
    private readonly Mock<IAnalyticsQueryRepository> _repo = new();
    private readonly MaterialUsageProjector _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string MaterialCode = "MDF-18";
    private static readonly DateOnly Date = new(2026, 4, 1);

    public MaterialUsageProjectorTests()
    {
        _sut = new MaterialUsageProjector(
            _gate.Object, _repo.Object,
            NullLogger<MaterialUsageProjector>.Instance);
    }

    [Fact]
    public async Task ProjectAsync_DuplicateEvent_RepoNotCalled()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), TenantId, default))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MaterialCode, Date, 1000m, 100m, 2, eventId, default);

        _repo.Verify(r => r.GetMaterialUsageAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProjectAsync_NewEvent_QueriesRepo()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, MaterialCode, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

        await _sut.ProjectAsync(TenantId, MaterialCode, Date, 1000m, 100m, 2, eventId, default);

        _repo.Verify(r => r.GetMaterialUsageAsync(TenantId, MaterialCode, Date, Date, 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_ExistingRecord_CallsUpdate()
    {
        var eventId = Guid.NewGuid();
        var existing = DailyMaterialUsage.Create(TenantId, MaterialCode, Date, 800m, 80m, 1);
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, MaterialCode, Date, Date, 0, 1, default))
            .ReturnsAsync(new[] { existing });

        await _sut.ProjectAsync(TenantId, MaterialCode, Date, 1200m, 200m, 3, eventId, default);

        existing.TotalAreaUsedMm2.Should().Be(1200m);
        existing.WasteAreaMm2.Should().Be(200m);
    }

    [Fact]
    public async Task ProjectAsync_WasteAreaExceedsTotal_CreateThrows()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, MaterialCode, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

        var act = async () => await _sut.ProjectAsync(TenantId, MaterialCode, Date, 100m, 200m, 0, eventId, default);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ProjectAsync_ZeroOffcutCount_IsValid()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, MaterialCode, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

        var act = async () => await _sut.ProjectAsync(TenantId, MaterialCode, Date, 500m, 50m, 0, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_MaterialCodePassedCorrectlyToRepo()
    {
        var eventId = Guid.NewGuid();
        const string code = "HDF-3";
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(TenantId, code, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

        await _sut.ProjectAsync(TenantId, code, Date, 500m, 50m, 1, eventId, default);

        _repo.Verify(r => r.GetMaterialUsageAsync(TenantId, code, Date, Date, 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_EmptyTenantId_GateCalledWithEmptyGuid()
    {
        // Guard is in DailyMaterialUsage.Create, not the projector itself
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(MaterialUsageProjector), Guid.Empty, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetMaterialUsageAsync(Guid.Empty, MaterialCode, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyMaterialUsage>());

        var act = async () => await _sut.ProjectAsync(Guid.Empty, MaterialCode, Date, 500m, 50m, 1, eventId, default);
        // DailyMaterialUsage.Create will throw for empty TenantId
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProjectAsync_GateSubscriberNameIsMaterialUsageProjector()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MaterialCode, Date, 500m, 50m, 1, eventId, default);

        _gate.Verify(g => g.IsAlreadyProcessedAsync(
            eventId, "MaterialUsageProjector", TenantId, default), Times.Once);
    }
}
