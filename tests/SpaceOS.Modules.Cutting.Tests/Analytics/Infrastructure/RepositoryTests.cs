using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Infrastructure;

public class RepositoryTests
{
    private static CuttingAnalyticsDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<CuttingAnalyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CuttingAnalyticsDbContext(opts);
    }

    // ══════════════════════════════════════════════════════════════════
    //  EfRebuildJobRepository (10 tests)
    // ══════════════════════════════════════════════════════════════════

    // 1. GetByIdAsync: existing → returns job
    [Fact]
    public async Task GetByIdAsync_ExistingJob_ReturnsJob()
    {
        await using var db = CreateContext();
        var job = AnalyticsRebuildJob.Create(Guid.NewGuid());
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var found = await repo.GetByIdAsync(job.Id, default);

        found.Should().NotBeNull();
        found!.Id.Should().Be(job.Id);
    }

    // 2. GetByIdAsync: missing → null
    [Fact]
    public async Task GetByIdAsync_MissingJob_ReturnsNull()
    {
        await using var db = CreateContext();
        var repo = new EfRebuildJobRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), default);

        result.Should().BeNull();
    }

    // 3. GetActiveForTenantAsync: Pending → returns
    [Fact]
    public async Task GetActiveForTenantAsync_PendingJob_ReturnsJob()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(tenantId);
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var active = await repo.GetActiveForTenantAsync(tenantId, default);

        active.Should().NotBeNull();
        active!.TenantId.Should().Be(tenantId);
    }

    // 4. GetActiveForTenantAsync: Running → returns
    [Fact]
    public async Task GetActiveForTenantAsync_RunningJob_ReturnsJob()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(tenantId);
        job.Start(10);
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var active = await repo.GetActiveForTenantAsync(tenantId, default);

        active.Should().NotBeNull();
        active!.Status.Should().Be(RebuildJobStatus.Running);
    }

    // 5. GetActiveForTenantAsync: Completed → null
    [Fact]
    public async Task GetActiveForTenantAsync_CompletedJob_ReturnsNull()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(tenantId);
        job.Start(1);
        job.Complete();
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var active = await repo.GetActiveForTenantAsync(tenantId, default);

        active.Should().BeNull();
    }

    // 6. GetActiveForTenantAsync: different tenant → null
    [Fact]
    public async Task GetActiveForTenantAsync_DifferentTenant_ReturnsNull()
    {
        await using var db = CreateContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var job = AnalyticsRebuildJob.Create(tenantA);
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var active = await repo.GetActiveForTenantAsync(tenantB, default);

        active.Should().BeNull();
    }

    // 7. AddAsync + SaveChangesAsync → persisted
    [Fact]
    public async Task AddAsync_ThenSaveChanges_PersistsJob()
    {
        await using var db = CreateContext();
        var repo = new EfRebuildJobRepository(db);
        var job = AnalyticsRebuildJob.Create(Guid.NewGuid());

        await repo.AddAsync(job, default);
        await repo.SaveChangesAsync(default);

        db.AnalyticsRebuildJobs.Should().ContainSingle(j => j.Id == job.Id);
    }

    // 8. Add + Start + SaveChangesAsync → status persisted
    [Fact]
    public async Task AddAsync_StartJob_StatusPersisted()
    {
        await using var db = CreateContext();
        var repo = new EfRebuildJobRepository(db);
        var job = AnalyticsRebuildJob.Create(Guid.NewGuid());
        await repo.AddAsync(job, default);
        await repo.SaveChangesAsync(default);

        job.Start(5);
        await repo.SaveChangesAsync(default);

        var found = await repo.GetByIdAsync(job.Id, default);
        found!.Status.Should().Be(RebuildJobStatus.Running);
    }

    // 9. Two jobs same tenant: one Pending, one Completed → GetActive returns Pending
    [Fact]
    public async Task GetActiveForTenantAsync_TwoJobs_ReturnsPending()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var completed = AnalyticsRebuildJob.Create(tenantId);
        completed.Start(1);
        completed.Complete();
        var pending = AnalyticsRebuildJob.Create(tenantId);
        db.AnalyticsRebuildJobs.AddRange(completed, pending);
        await db.SaveChangesAsync();

        var repo = new EfRebuildJobRepository(db);
        var active = await repo.GetActiveForTenantAsync(tenantId, default);

        active.Should().NotBeNull();
        active!.Status.Should().Be(RebuildJobStatus.Pending);
    }

    // 10. GetByIdAsync returns null for empty Guid
    [Fact]
    public async Task GetByIdAsync_EmptyGuid_ReturnsNull()
    {
        await using var db = CreateContext();
        var repo = new EfRebuildJobRepository(db);

        var result = await repo.GetByIdAsync(Guid.Empty, default);

        result.Should().BeNull();
    }

    // ══════════════════════════════════════════════════════════════════
    //  EfAnalyticsQueryRepository (10 tests)
    // ══════════════════════════════════════════════════════════════════

    private static DailyExecutionMetric MakeExecMetric(Guid tenantId, string machineId, DateOnly date)
        => DailyExecutionMetric.Create(tenantId, machineId, date, 5, 15.0m, 92.0m);

    private static DailyMaterialUsage MakeMaterialUsage(Guid tenantId, string code, DateOnly date)
        => DailyMaterialUsage.Create(tenantId, code, date, 1000m, 100m, 2);

    private static MachineOEEHourly MakeOEE(Guid tenantId, string machineId, DateTime hourSlot)
    {
        var score = OEEScore.Create(0.9m, 0.85m, 0.95m).Value;
        return MachineOEEHourly.Create(tenantId, machineId, hourSlot, score);
    }

    private static DailyOperatorMetric MakeOperator(Guid tenantId, bool suppressed = false)
    {
        var m = DailyOperatorMetric.Create(tenantId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), 3, 20m);
        if (suppressed) m.Suppress();
        return m;
    }

    // 11. GetExecutionMetricsAsync: filter by tenant
    [Fact]
    public async Task GetExecutionMetricsAsync_FiltersByTenant()
    {
        await using var db = CreateContext();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyExecutionMetrics.AddRange(
            MakeExecMetric(tenantA, "M1", today),
            MakeExecMetric(tenantB, "M2", today));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetExecutionMetricsAsync(tenantA, null, today.AddDays(-1), today.AddDays(1), 0, 10, default);

        result.Should().HaveCount(1);
        result[0].TenantId.Should().Be(tenantA);
    }

    // 12. GetExecutionMetricsAsync: filter by machineId
    [Fact]
    public async Task GetExecutionMetricsAsync_FiltersByMachineId()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyExecutionMetrics.AddRange(
            MakeExecMetric(tenantId, "M1", today),
            MakeExecMetric(tenantId, "M2", today));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetExecutionMetricsAsync(tenantId, "M1", today.AddDays(-1), today.AddDays(1), 0, 10, default);

        result.Should().HaveCount(1);
        result[0].MachineId.Should().Be("M1");
    }

    // 13. GetExecutionMetricsAsync: date range filter
    [Fact]
    public async Task GetExecutionMetricsAsync_DateRangeFilter_ExcludesOutsideRange()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyExecutionMetrics.AddRange(
            MakeExecMetric(tenantId, "M1", today),
            MakeExecMetric(tenantId, "M1", today.AddDays(-40)));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetExecutionMetricsAsync(tenantId, null, today.AddDays(-5), today.AddDays(1), 0, 10, default);

        result.Should().HaveCount(1);
        result[0].MetricDate.Should().Be(today);
    }

    // 14. GetExecutionMetricsAsync: skip/take pagination
    [Fact]
    public async Task GetExecutionMetricsAsync_SkipTakePagination()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var base_ = DateOnly.FromDateTime(DateTime.Today);
        for (var i = 0; i < 5; i++)
            db.DailyExecutionMetrics.Add(MakeExecMetric(tenantId, $"M{i}", base_.AddDays(-i)));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetExecutionMetricsAsync(tenantId, null, base_.AddDays(-10), base_.AddDays(1), 2, 2, default);

        result.Should().HaveCount(2);
    }

    // 15. GetMaterialUsageAsync: filter by materialCode
    [Fact]
    public async Task GetMaterialUsageAsync_FiltersByMaterialCode()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyMaterialUsages.AddRange(
            MakeMaterialUsage(tenantId, "MDF-18", today),
            MakeMaterialUsage(tenantId, "HDF-3", today));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetMaterialUsageAsync(tenantId, "MDF-18", today.AddDays(-1), today.AddDays(1), 0, 10, default);

        result.Should().HaveCount(1);
        result[0].MaterialCode.Should().Be("MDF-18");
    }

    // 16. GetMaterialUsageAsync: date range filter
    [Fact]
    public async Task GetMaterialUsageAsync_DateRangeFilter_ExcludesOldRecords()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyMaterialUsages.AddRange(
            MakeMaterialUsage(tenantId, "MDF-18", today),
            MakeMaterialUsage(tenantId, "MDF-18", today.AddDays(-100)));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetMaterialUsageAsync(tenantId, null, today.AddDays(-5), today, 0, 10, default);

        result.Should().HaveCount(1);
    }

    // 17. GetOEEAsync: filter by machineId
    [Fact]
    public async Task GetOEEAsync_FiltersByMachineId()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        db.MachineOEEHourlies.AddRange(
            MakeOEE(tenantId, "CNC-1", now),
            MakeOEE(tenantId, "CNC-2", now));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetOEEAsync(tenantId, "CNC-1", now.AddHours(-1), now.AddHours(1), 0, 10, default);

        result.Should().HaveCount(1);
        result[0].MachineId.Should().Be("CNC-1");
    }

    // 18. GetOperatorMetricsAnonymizedAsync: suppressed excluded
    [Fact]
    public async Task GetOperatorMetricsAnonymizedAsync_SuppressedExcluded()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyOperatorMetrics.AddRange(
            MakeOperator(tenantId, suppressed: true),
            MakeOperator(tenantId, suppressed: false));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var policy = AnonymizationPolicy.Default;
        var result = await repo.GetOperatorMetricsAnonymizedAsync(tenantId, today.AddDays(-1), today.AddDays(1), policy, 0, 10, default);

        result.Should().HaveCount(1);
        result[0].IsSuppressed.Should().BeFalse();
    }

    // 19. GetOperatorMetricsAnonymizedAsync: non-suppressed included
    [Fact]
    public async Task GetOperatorMetricsAnonymizedAsync_NonSuppressedIncluded()
    {
        await using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        db.DailyOperatorMetrics.Add(MakeOperator(tenantId, suppressed: false));
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var result = await repo.GetOperatorMetricsAnonymizedAsync(tenantId, today.AddDays(-1), today.AddDays(1), AnonymizationPolicy.Default, 0, 10, default);

        result.Should().HaveCount(1);
    }

    // 20. GetRebuildJobAsync: existing → returns
    [Fact]
    public async Task GetRebuildJobAsync_ExistingJob_Returns()
    {
        await using var db = CreateContext();
        var job = AnalyticsRebuildJob.Create(Guid.NewGuid());
        db.AnalyticsRebuildJobs.Add(job);
        await db.SaveChangesAsync();

        var repo = new EfAnalyticsQueryRepository(db);
        var found = await repo.GetRebuildJobAsync(job.Id, default);

        found.Should().NotBeNull();
        found!.Id.Should().Be(job.Id);
    }
}
