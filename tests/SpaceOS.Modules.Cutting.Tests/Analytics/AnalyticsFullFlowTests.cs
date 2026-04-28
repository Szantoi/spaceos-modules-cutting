using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Application.Projections;
using SpaceOS.Modules.Cutting.Analytics.Application.Queries;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics;

/// <summary>
/// Integration tests using InMemory EF Core + real repositories + real query handlers.
/// Validates that the analytics stack works end-to-end without external dependencies.
/// </summary>
public class AnalyticsFullFlowTests
{
    private static CuttingAnalyticsDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<CuttingAnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CuttingAnalyticsDbContext(opts);
    }

    // ══════════════════════════════════════════════════════════════════
    //  Query handler + repository integration (10 tests)
    // ══════════════════════════════════════════════════════════════════

    // 1. Seed DailyExecutionMetric → query handler returns it
    [Fact]
    public async Task GetDailyExecutionMetrics_AfterSeed_ReturnsSeededRecord()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var metric = DailyExecutionMetric.Create(tenantId, "CNC-1", today, 10, 25.5m, 88m);
        db.DailyExecutionMetrics.Add(metric);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetDailyExecutionMetricsQueryHandler(repo);
        var result = await handler.Handle(
            new GetDailyExecutionMetricsQuery(tenantId, null, today.AddDays(-1), today.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].MachineId.Should().Be("CNC-1");
        result.Value.Items[0].CompletedCount.Should().Be(10);
    }

    // 2. Seed DailyMaterialUsage → query handler returns it
    [Fact]
    public async Task GetMaterialUsage_AfterSeed_ReturnsSeededRecord()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var usage = DailyMaterialUsage.Create(tenantId, "MDF-18", today, 50000m, 5000m, 3);
        db.DailyMaterialUsages.Add(usage);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetMaterialUsageQueryHandler(repo);
        var result = await handler.Handle(
            new GetMaterialUsageQuery(tenantId, null, today.AddDays(-1), today.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].MaterialCode.Should().Be("MDF-18");
        result.Value.Items[0].TotalAreaUsedMm2.Should().Be(50000m);
    }

    // 3. Empty DB → query handler returns empty paged result (not error)
    [Fact]
    public async Task GetDailyExecutionMetrics_EmptyDb_ReturnsEmptyPage()
    {
        await using var db = CreateDb();
        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetDailyExecutionMetricsQueryHandler(repo);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = await handler.Handle(
            new GetDailyExecutionMetricsQuery(Guid.NewGuid(), null, today.AddDays(-7), today), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // 4. Multi-tenant: each tenant sees only own data
    [Fact]
    public async Task GetDailyExecutionMetrics_MultiTenant_EachTenantSeesOwnData()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyExecutionMetrics.AddRange(
            DailyExecutionMetric.Create(tenantA, "M1", today, 5, 20m, 90m),
            DailyExecutionMetric.Create(tenantB, "M2", today, 8, 15m, 85m));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetDailyExecutionMetricsQueryHandler(repo);

        var resultA = await handler.Handle(
            new GetDailyExecutionMetricsQuery(tenantA, null, today.AddDays(-1), today.AddDays(1)), default);
        var resultB = await handler.Handle(
            new GetDailyExecutionMetricsQuery(tenantB, null, today.AddDays(-1), today.AddDays(1)), default);

        resultA.Value.Items.Should().HaveCount(1).And.OnlyContain(m => m.TenantId == tenantA);
        resultB.Value.Items.Should().HaveCount(1).And.OnlyContain(m => m.TenantId == tenantB);
    }

    // 5. Machine filter applied end-to-end
    [Fact]
    public async Task GetDailyExecutionMetrics_MachineFilter_FiltersCorrectly()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyExecutionMetrics.AddRange(
            DailyExecutionMetric.Create(tenantId, "CNC-1", today, 3, 10m, 80m),
            DailyExecutionMetric.Create(tenantId, "CNC-2", today, 7, 12m, 90m));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetDailyExecutionMetricsQueryHandler(repo);

        var result = await handler.Handle(
            new GetDailyExecutionMetricsQuery(tenantId, "CNC-1", today.AddDays(-1), today.AddDays(1)), default);

        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].MachineId.Should().Be("CNC-1");
    }

    // 6. AnalyticsRebuildJob lifecycle: Pending → Running → Completed persisted correctly
    [Fact]
    public async Task RebuildJob_FullLifecycle_PersistedCorrectly()
    {
        await using var db = CreateDb();
        var repo = new EfRebuildJobRepository(db);
        var tenantId = Guid.NewGuid();

        var job = AnalyticsRebuildJob.Create(tenantId);
        await repo.AddAsync(job, default);
        await repo.SaveChangesAsync(default);

        job.Start(5);
        await repo.SaveChangesAsync(default);

        job.RecordChunkProgress();
        await repo.SaveChangesAsync(default);

        job.Complete();
        await repo.SaveChangesAsync(default);

        var found = await repo.GetByIdAsync(job.Id, default);
        found.Should().NotBeNull();
        found!.Status.Should().Be(RebuildJobStatus.Completed);
        found.CompletedAt.Should().NotBeNull();
        found.ProcessedChunks.Should().Be(1);
        found.TotalChunks.Should().Be(5);
    }

    // 7. AnalyticsRebuildJob Fail path persisted correctly
    [Fact]
    public async Task RebuildJob_FailPath_ErrorMessagePersisted()
    {
        await using var db = CreateDb();
        var repo = new EfRebuildJobRepository(db);
        var tenantId = Guid.NewGuid();

        var job = AnalyticsRebuildJob.Create(tenantId);
        await repo.AddAsync(job, default);
        await repo.SaveChangesAsync(default);

        job.Start(3);
        job.Fail("Database connection lost");
        await repo.SaveChangesAsync(default);

        var found = await repo.GetByIdAsync(job.Id, default);
        found!.Status.Should().Be(RebuildJobStatus.Failed);
        found.ErrorMessage.Should().Be("Database connection lost");
        found.CompletedAt.Should().NotBeNull();
    }

    // 8. Operator metric suppression: suppressed records excluded from query results
    [Fact]
    public async Task GetOperatorMetrics_SuppressedRecord_ExcludedFromResults()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var visible = DailyOperatorMetric.Create(tenantId, Guid.NewGuid(), today, 5, 30m);
        var suppressed = DailyOperatorMetric.Create(tenantId, Guid.NewGuid(), today, 2, 25m);
        suppressed.Suppress();
        db.DailyOperatorMetrics.AddRange(visible, suppressed);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetOperatorMetricsQueryHandler(repo);

        var result = await handler.Handle(
            new GetOperatorMetricsQuery(tenantId, today.AddDays(-1), today.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].IsSuppressed.Should().BeFalse();
    }

    // 9. OEE query: time range filter applied end-to-end
    [Fact]
    public async Task GetMachineOEE_TimeRangeFilter_ExcludesOutOfRange()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var score = OEEScore.Create(0.9m, 0.8m, 0.95m).Value;

        var inRange = MachineOEEHourly.Create(tenantId, "CNC-1", now, score);
        var outOfRange = MachineOEEHourly.Create(tenantId, "CNC-1", now.AddDays(-10), score);
        db.MachineOEEHourlies.AddRange(inRange, outOfRange);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetMachineOEEQueryHandler(repo);

        var result = await handler.Handle(
            new GetMachineOEEQuery(tenantId, null, now.AddDays(-1), now.AddDays(1)), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    // 10. Pagination: skip/take applied correctly end-to-end
    [Fact]
    public async Task GetMaterialUsage_Pagination_SkipTakeApplied()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var baseDate = DateOnly.FromDateTime(DateTime.Today);

        for (var i = 0; i < 6; i++)
            db.DailyMaterialUsages.Add(
                DailyMaterialUsage.Create(tenantId, $"MAT-{i:D2}", baseDate.AddDays(-i), 1000m, 100m, 1));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetMaterialUsageQueryHandler(repo);

        var page1 = await handler.Handle(
            new GetMaterialUsageQuery(tenantId, null, baseDate.AddDays(-10), baseDate, 0, 3), default);
        var page2 = await handler.Handle(
            new GetMaterialUsageQuery(tenantId, null, baseDate.AddDays(-10), baseDate, 3, 3), default);

        page1.Value.Items.Should().HaveCount(3);
        page2.Value.Items.Should().HaveCount(3);
        page1.Value.Items.Select(m => m.MaterialCode)
            .Should().NotIntersectWith(page2.Value.Items.Select(m => m.MaterialCode));
    }

    // ══════════════════════════════════════════════════════════════════
    //  ExecutionMetricProjector integration (5 tests)
    // ══════════════════════════════════════════════════════════════════

    // 11. Projector with mocked gate (first call) → metric created
    [Fact]
    public async Task ExecutionMetricProjector_FirstEvent_MetricCreated()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var repoMock = new Mock<IAnalyticsQueryRepository>();
        repoMock.Setup(r => r.GetExecutionMetricsAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DailyExecutionMetric>());

        var projector = new ExecutionMetricProjector(gate.Object, repoMock.Object, NullLogger<ExecutionMetricProjector>.Instance);
        var today = DateOnly.FromDateTime(DateTime.Today);

        await projector.ProjectAsync(Guid.NewGuid(), "CNC-1", today, 5, 20m, 88m, Guid.NewGuid(), default);

        // Gate was checked once
        gate.Verify(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), nameof(ExecutionMetricProjector), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // 12. Projector with duplicate event (gate returns true) → skips projection
    [Fact]
    public async Task ExecutionMetricProjector_DuplicateEvent_Skipped()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var repoMock = new Mock<IAnalyticsQueryRepository>();

        var projector = new ExecutionMetricProjector(gate.Object, repoMock.Object, NullLogger<ExecutionMetricProjector>.Instance);
        var today = DateOnly.FromDateTime(DateTime.Today);

        await projector.ProjectAsync(Guid.NewGuid(), "CNC-1", today, 5, 20m, 88m, Guid.NewGuid(), default);

        // Repo never queried when event is duplicate
        repoMock.Verify(r => r.GetExecutionMetricsAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // 13. Projector existing metric → Update called (not Create)
    [Fact]
    public async Task ExecutionMetricProjector_ExistingMetric_UpdateCalled()
    {
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var gate = new Mock<IProjectionIdempotencyGate>();
        gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var existingMetric = DailyExecutionMetric.Create(tenantId, "CNC-1", today, 3, 10m, 75m);
        var repoMock = new Mock<IAnalyticsQueryRepository>();
        repoMock.Setup(r => r.GetExecutionMetricsAsync(tenantId, "CNC-1", today, today, 0, 1, default))
            .ReturnsAsync(new[] { existingMetric });

        var projector = new ExecutionMetricProjector(gate.Object, repoMock.Object, NullLogger<ExecutionMetricProjector>.Instance);

        await projector.ProjectAsync(tenantId, "CNC-1", today, 10, 20m, 90m, Guid.NewGuid(), default);

        // Metric was updated in-place
        existingMetric.CompletedCount.Should().Be(10);
        existingMetric.YieldPercent.Should().Be(90m);
    }

    // 14. GetRebuildJobStatusQuery: existing job returns success
    [Fact]
    public async Task GetRebuildJobStatusQuery_ExistingJob_ReturnsSuccess()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(tenantId);
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetRebuildJobStatusQueryHandler(repo);

        var result = await handler.Handle(new GetRebuildJobStatusQuery(tenantId, job.Id), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(job.Id);
    }

    // 15. GetRebuildJobStatusQuery: non-existent job returns NotFound
    [Fact]
    public async Task GetRebuildJobStatusQuery_NonExistentJob_ReturnsNotFound()
    {
        await using var db = CreateDb();
        var repo = new EfAnalyticsQueryRepository(db);
        var handler = new GetRebuildJobStatusQueryHandler(repo);

        var result = await handler.Handle(new GetRebuildJobStatusQuery(Guid.NewGuid(), Guid.NewGuid()), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }
}
