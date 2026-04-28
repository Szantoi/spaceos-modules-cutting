using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Application;

public class OEEProjectorTests
{
    private readonly Mock<IProjectionIdempotencyGate> _gate = new();
    private readonly Mock<IAnalyticsQueryRepository> _repo = new();
    private readonly OEEProjector _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string MachineId = "M1";
    private static readonly DateTime HourSlot = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly OEEScore Score = OEEScore.Create(0.9m, 0.8m, 0.95m).Value;

    public OEEProjectorTests()
    {
        _sut = new OEEProjector(
            _gate.Object, _repo.Object,
            NullLogger<OEEProjector>.Instance);
    }

    [Fact]
    public async Task ProjectAsync_DuplicateEvent_RepoNotCalled()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(OEEProjector), TenantId, default))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, Score, eventId, default);

        _repo.Verify(r => r.GetOEEAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProjectAsync_NewEvent_QueriesRepo()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(OEEProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default))
            .ReturnsAsync(Array.Empty<MachineOEEHourly>());

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, Score, eventId, default);

        _repo.Verify(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_ScorePassedCorrectly_UpdatesExistingRecord()
    {
        var eventId = Guid.NewGuid();
        var newScore = OEEScore.Create(0.5m, 0.5m, 0.5m).Value;
        var existing = MachineOEEHourly.Create(TenantId, MachineId, HourSlot, Score);
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(OEEProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default))
            .ReturnsAsync(new[] { existing });

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, newScore, eventId, default);

        existing.Score.Should().Be(newScore);
    }

    [Fact]
    public async Task ProjectAsync_HourSlotWindowUsedForRepoQuery()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default))
            .ReturnsAsync(Array.Empty<MachineOEEHourly>());

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, Score, eventId, default);

        _repo.Verify(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_GateSubscriberNameIsOEEProjector()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, Score, eventId, default);

        _gate.Verify(g => g.IsAlreadyProcessedAsync(eventId, "OEEProjector", TenantId, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_OverallComputedByValueObject_NotProjector()
    {
        // OEE Overall must come from OEEScore.Overall, not any projector logic
        var score = OEEScore.Create(0.6m, 0.7m, 0.8m).Value;
        score.Overall.Should().Be(0.6m * 0.7m * 0.8m);
    }

    [Fact]
    public async Task ProjectAsync_TenantIdIsolation_OnlyQueriesCorrectTenant()
    {
        var eventId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(OEEProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetOEEAsync(TenantId, MachineId, HourSlot, HourSlot.AddHours(1), 0, 1, default))
            .ReturnsAsync(Array.Empty<MachineOEEHourly>());

        await _sut.ProjectAsync(TenantId, MachineId, HourSlot, Score, eventId, default);

        _repo.Verify(r => r.GetOEEAsync(otherTenant, It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
