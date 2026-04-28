using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Application;

public class ExecutionMetricProjectorTests
{
    private readonly Mock<IProjectionIdempotencyGate> _gate = new();
    private readonly Mock<IAnalyticsQueryRepository> _repo = new();
    private readonly ExecutionMetricProjector _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly string MachineId = "M1";
    private static readonly DateOnly Date = new(2026, 4, 1);

    public ExecutionMetricProjectorTests()
    {
        _sut = new ExecutionMetricProjector(
            _gate.Object, _repo.Object,
            NullLogger<ExecutionMetricProjector>.Instance);
    }

    [Fact]
    public async Task ProjectAsync_DuplicateEvent_RepoIsNotCalled()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), TenantId, default))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MachineId, Date, 5, 30m, 80m, eventId, default);

        _repo.Verify(r => r.GetExecutionMetricsAsync(
            It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProjectAsync_NewEvent_QueriesRepoForExistingMetric()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, MachineId, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        await _sut.ProjectAsync(TenantId, MachineId, Date, 5, 30m, 80m, eventId, default);

        _repo.Verify(r => r.GetExecutionMetricsAsync(TenantId, MachineId, Date, Date, 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_ExistingMetric_CallsUpdate()
    {
        var eventId = Guid.NewGuid();
        var existing = DailyExecutionMetric.Create(TenantId, MachineId, Date, 3, 25m, 75m);
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, MachineId, Date, Date, 0, 1, default))
            .ReturnsAsync(new[] { existing });

        await _sut.ProjectAsync(TenantId, MachineId, Date, 10, 35m, 90m, eventId, default);

        existing.CompletedCount.Should().Be(10);
        existing.YieldPercent.Should().Be(90m);
    }

    [Fact]
    public async Task ProjectAsync_GateCalledWithCorrectArguments()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.ProjectAsync(TenantId, MachineId, Date, 5, 30m, 80m, eventId, default);

        _gate.Verify(g => g.IsAlreadyProcessedAsync(
            eventId, nameof(ExecutionMetricProjector), TenantId, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_ZeroCompletedCount_IsValid()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, MachineId, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        var act = async () => await _sut.ProjectAsync(TenantId, MachineId, Date, 0, 0m, 0m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_FullYield_IsValid()
    {
        var eventId = Guid.NewGuid();
        _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(ExecutionMetricProjector), TenantId, default))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, MachineId, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        var act = async () => await _sut.ProjectAsync(TenantId, MachineId, Date, 1, 20m, 100m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_MachineIdPassedCorrectlyToRepo()
    {
        var eventId = Guid.NewGuid();
        const string specificMachine = "LASER-42";
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, specificMachine, Date, Date, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        await _sut.ProjectAsync(TenantId, specificMachine, Date, 1, 10m, 90m, eventId, default);

        _repo.Verify(r => r.GetExecutionMetricsAsync(TenantId, specificMachine, Date, Date, 0, 1, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_DatePassedCorrectlyToRepo()
    {
        var eventId = Guid.NewGuid();
        var specificDate = new DateOnly(2026, 12, 31);
        _gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repo.Setup(r => r.GetExecutionMetricsAsync(TenantId, MachineId, specificDate, specificDate, 0, 1, default))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        await _sut.ProjectAsync(TenantId, MachineId, specificDate, 1, 10m, 90m, eventId, default);

        _repo.Verify(r => r.GetExecutionMetricsAsync(TenantId, MachineId, specificDate, specificDate, 0, 1, default), Times.Once);
    }
}
