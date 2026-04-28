using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Analytics.Domain.Common;
using SpaceOS.Modules.Cutting.Analytics.Domain.ReadModels;
using SpaceOS.Modules.Cutting.Analytics.Domain.Specifications;
using SpaceOS.Modules.Cutting.Analytics.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Domain;

/// <summary>
/// Tests Specification WHERE expressions by evaluating them directly via LINQ.
/// Ardalis.Specification WhereExpressions are plain Func&lt;T, bool&gt; predicates and
/// can be composed without an EF Core or in-memory evaluator.
/// </summary>
public class SpecificationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenant = Guid.NewGuid();

    private static bool MatchesAll<T>(T item, Ardalis.Specification.Specification<T> spec)
    {
        var predicates = spec.WhereExpressions.Select(we => we.Filter.Compile()).ToList();
        return predicates.All(p => p(item));
    }

    // ── DailyExecutionMetricByDateRangeSpec ──────────────────────────────────

    [Fact]
    public void DailyExecutionMetricByDateRangeSpec_Matches_RecordInRange()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyExecutionMetric.Create(TenantId, "M1", date, 10, 30m, 80m);
        var spec = new DailyExecutionMetricByDateRangeSpec(TenantId, date, date, null);

        MatchesAll(metric, spec).Should().BeTrue();
    }

    [Fact]
    public void DailyExecutionMetricByDateRangeSpec_DoesNotMatch_RecordOutsideRange()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyExecutionMetric.Create(TenantId, "M1", date.AddDays(5), 10, 30m, 80m);
        var spec = new DailyExecutionMetricByDateRangeSpec(TenantId, date, date, null);

        MatchesAll(metric, spec).Should().BeFalse();
    }

    // ── DailyMaterialUsageByDateRangeSpec ────────────────────────────────────

    [Fact]
    public void DailyMaterialUsageByDateRangeSpec_Matches_RecordInRange()
    {
        var date = new DateOnly(2026, 4, 1);
        var usage = DailyMaterialUsage.Create(TenantId, "MDF-18", date, 1000m, 100m, 2);
        var spec = new DailyMaterialUsageByDateRangeSpec(TenantId, date, date, null);

        MatchesAll(usage, spec).Should().BeTrue();
    }

    [Fact]
    public void DailyMaterialUsageByDateRangeSpec_DoesNotMatch_WrongMaterialCode()
    {
        var date = new DateOnly(2026, 4, 1);
        var usage = DailyMaterialUsage.Create(TenantId, "MDF-18", date, 1000m, 100m, 2);
        var spec = new DailyMaterialUsageByDateRangeSpec(TenantId, date, date, "HDF-3");

        MatchesAll(usage, spec).Should().BeFalse();
    }

    // ── MachineOEEHourlyByDateRangeSpec ──────────────────────────────────────

    [Fact]
    public void MachineOEEHourlyByDateRangeSpec_Matches_RecordInRange()
    {
        var score = OEEScore.Create(0.9m, 0.8m, 0.95m).Value;
        var hourSlot = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var oee = MachineOEEHourly.Create(TenantId, "M1", hourSlot, score);
        var spec = new MachineOEEHourlyByDateRangeSpec(TenantId, hourSlot, hourSlot.AddHours(1), null);

        MatchesAll(oee, spec).Should().BeTrue();
    }

    [Fact]
    public void MachineOEEHourlyByDateRangeSpec_DoesNotMatch_WrongMachine()
    {
        var score = OEEScore.Create(0.9m, 0.8m, 0.95m).Value;
        var hourSlot = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var oee = MachineOEEHourly.Create(TenantId, "M1", hourSlot, score);
        var spec = new MachineOEEHourlyByDateRangeSpec(TenantId, hourSlot, hourSlot.AddHours(1), "M2");

        MatchesAll(oee, spec).Should().BeFalse();
    }

    // ── DailyOperatorMetricByDateRangeSpec ───────────────────────────────────

    [Fact]
    public void DailyOperatorMetricByDateRangeSpec_IncludesSuppressedRecord()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyOperatorMetric.Create(TenantId, Guid.NewGuid(), date, 5, 45m);
        metric.Suppress();
        var spec = new DailyOperatorMetricByDateRangeSpec(TenantId, date, date);

        MatchesAll(metric, spec).Should().BeTrue();
    }

    [Fact]
    public void DailyOperatorMetricByDateRangeSpec_DoesNotMatch_WrongTenant()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyOperatorMetric.Create(OtherTenant, Guid.NewGuid(), date, 5, 45m);
        var spec = new DailyOperatorMetricByDateRangeSpec(TenantId, date, date);

        MatchesAll(metric, spec).Should().BeFalse();
    }

    // ── DailyOperatorMetricAnonymizedSpec ────────────────────────────────────

    [Fact]
    public void DailyOperatorMetricAnonymizedSpec_ExcludesSuppressed()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyOperatorMetric.Create(TenantId, Guid.NewGuid(), date, 5, 45m);
        metric.Suppress();
        var spec = new DailyOperatorMetricAnonymizedSpec(TenantId, date, date);

        MatchesAll(metric, spec).Should().BeFalse();
    }

    [Fact]
    public void DailyOperatorMetricAnonymizedSpec_IncludesNonSuppressed()
    {
        var date = new DateOnly(2026, 4, 1);
        var metric = DailyOperatorMetric.Create(TenantId, Guid.NewGuid(), date, 5, 45m);
        var spec = new DailyOperatorMetricAnonymizedSpec(TenantId, date, date);

        MatchesAll(metric, spec).Should().BeTrue();
    }

    // ── AnalyticsRebuildJobActiveSpec ────────────────────────────────────────

    [Fact]
    public void AnalyticsRebuildJobActiveSpec_IncludesPending()
    {
        var pending = AnalyticsRebuildJob.Create(TenantId);
        var spec = new AnalyticsRebuildJobActiveSpec(TenantId);

        MatchesAll(pending, spec).Should().BeTrue();
    }

    [Fact]
    public void AnalyticsRebuildJobActiveSpec_ExcludesCompleted()
    {
        var job = AnalyticsRebuildJob.Create(TenantId);
        job.Start(1);
        job.Complete();
        var spec = new AnalyticsRebuildJobActiveSpec(TenantId);

        MatchesAll(job, spec).Should().BeFalse();
    }

    // ── AnalyticsRebuildJobByStatusSpec ─────────────────────────────────────

    [Fact]
    public void AnalyticsRebuildJobByStatusSpec_MatchesPendingStatus()
    {
        var pending = AnalyticsRebuildJob.Create(TenantId);
        var spec = new AnalyticsRebuildJobByStatusSpec(TenantId, RebuildJobStatus.Pending);

        MatchesAll(pending, spec).Should().BeTrue();
    }

    [Fact]
    public void AnalyticsRebuildJobByStatusSpec_DoesNotMatchDifferentStatus()
    {
        var pending = AnalyticsRebuildJob.Create(TenantId);
        var spec = new AnalyticsRebuildJobByStatusSpec(TenantId, RebuildJobStatus.Running);

        MatchesAll(pending, spec).Should().BeFalse();
    }

    // ── AnalyticsPagedResult.HasNextPage ─────────────────────────────────────

    [Fact]
    public void AnalyticsPagedResult_HasNextPage_WhenItemsLessThanTotal()
    {
        var paged = new AnalyticsPagedResult<DailyExecutionMetric>(
            Array.Empty<DailyExecutionMetric>(), 50, 0, 10);
        paged.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void AnalyticsPagedResult_NoNextPage_WhenAllItemsReturned()
    {
        var metric = DailyExecutionMetric.Create(Guid.NewGuid(), "M1", new DateOnly(2026, 1, 1), 1, 10m, 90m);
        var paged = new AnalyticsPagedResult<DailyExecutionMetric>(
            new[] { metric }, 1, 0, 10);
        paged.HasNextPage.Should().BeFalse();
    }
}
