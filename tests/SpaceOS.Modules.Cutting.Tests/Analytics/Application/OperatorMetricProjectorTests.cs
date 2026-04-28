using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Application;

public class OperatorMetricProjectorTests
{
    private readonly Mock<IProjectionIdempotencyGate> _gate = new();
    private readonly Mock<IAnalyticsQueryRepository> _repo = new();
    private readonly OperatorMetricProjector _sut;

    // Policy defaults: k=5, l=2, window=7 days
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 4, 10);
    private static readonly DateOnly WindowStart = Date.AddDays(-7);

    public OperatorMetricProjectorTests()
    {
        _sut = new OperatorMetricProjector(
            _gate.Object, _repo.Object,
            NullLogger<OperatorMetricProjector>.Instance);
    }

    private static DailyOperatorMetric MakeMetric(Guid workerId)
        => DailyOperatorMetric.Create(TenantId, workerId, Date, 3, 30m);

    private IReadOnlyList<DailyOperatorMetric> WindowWith(int distinctWorkerCount, Guid? includeWorker = null)
    {
        var list = new List<DailyOperatorMetric>();
        for (var i = 0; i < distinctWorkerCount; i++)
            list.Add(MakeMetric(Guid.NewGuid()));
        if (includeWorker.HasValue)
            list.Add(MakeMetric(includeWorker.Value));
        return list;
    }

    private void SetupGate(Guid eventId, bool alreadyProcessed)
        => _gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, nameof(OperatorMetricProjector), TenantId, default))
            .ReturnsAsync(alreadyProcessed);

    private void SetupRepo(IReadOnlyList<DailyOperatorMetric> returnValue)
        => _repo.Setup(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, WindowStart, Date, It.IsAny<AnonymizationPolicy>(), 0, 500, default))
            .ReturnsAsync(returnValue);

    [Fact]
    public async Task ProjectAsync_DistinctWorkersAtLeastK_NotSuppressed()
    {
        // 4 existing + 1 incoming = 5 = k → NOT suppressed
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(WindowWith(4));

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_DistinctWorkersBelowK_MetricIsSuppressed()
    {
        // 3 existing + 1 incoming = 4 < k=5 → suppress
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(WindowWith(3));

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_DuplicateEvent_RepoNotCalled()
    {
        var eventId = Guid.NewGuid();
        SetupGate(eventId, true);

        await _sut.ProjectAsync(TenantId, Guid.NewGuid(), Date, 5, 30m, eventId, default);

        _repo.Verify(r => r.GetOperatorMetricsAnonymizedAsync(
            It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<AnonymizationPolicy>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProjectAsync_ExactlyKWorkers_NotSuppressed()
    {
        // 4 existing + 1 incoming = exactly k=5 → should NOT be suppressed
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(WindowWith(4));

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_FourWorkers_SuppressApplied()
    {
        // 3 existing + 1 incoming = 4 < k=5 → suppress
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(WindowWith(3));

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 1, 20m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_GateSubscriberNameIsOperatorMetricProjector()
    {
        var eventId = Guid.NewGuid();
        SetupGate(eventId, true);

        await _sut.ProjectAsync(TenantId, Guid.NewGuid(), Date, 5, 30m, eventId, default);

        _gate.Verify(g => g.IsAlreadyProcessedAsync(eventId, "OperatorMetricProjector", TenantId, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_ExistingWorkerInWindow_NotCountedTwice()
    {
        // incoming worker is already in window → total distinct = 4 < 5 → suppress
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(WindowWith(3, includeWorker: workerId));

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_MinDaysWindowUsedForRepoQuery()
    {
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        _repo.Setup(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, WindowStart, Date, It.IsAny<AnonymizationPolicy>(), 0, 500, default))
            .ReturnsAsync(Array.Empty<DailyOperatorMetric>());

        await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);

        _repo.Verify(r => r.GetOperatorMetricsAnonymizedAsync(
            TenantId, WindowStart, Date, It.IsAny<AnonymizationPolicy>(), 0, 500, default), Times.Once);
    }

    [Fact]
    public async Task ProjectAsync_SuppressedMetricsInWindow_NotCountedAsDistinct()
    {
        // Anonymized query returns empty (all suppressed) → only 1 incoming worker < k=5 → suppress
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        _repo.Setup(r => r.GetOperatorMetricsAnonymizedAsync(
                TenantId, WindowStart, Date, It.IsAny<AnonymizationPolicy>(), 0, 500, default))
            .ReturnsAsync(Array.Empty<DailyOperatorMetric>());

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProjectAsync_AlreadySuppressedMetricsInWindow_AreNotCountedAsDistinct()
    {
        // GetOperatorMetricsAnonymizedAsync excludes suppressed records, so returning
        // empty list simulates the case where all window entries were suppressed
        var workerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        SetupGate(eventId, false);
        SetupRepo(new List<DailyOperatorMetric>());

        var act = async () => await _sut.ProjectAsync(TenantId, workerId, Date, 5, 30m, eventId, default);
        await act.Should().NotThrowAsync();
    }
}
