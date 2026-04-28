using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Events;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using SpaceOS.Modules.Cutting.Execution.Application.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class MilestonePredicateTests
{
    private static WorkerAssignment ValidWorker() =>
        WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;

    private static ScheduleWindow WindowContaining(DateTime moment) =>
        ScheduleWindow.Create(moment.AddHours(-1), moment.AddHours(1)).Value;

    private static IWorkerSecurityPolicy AlwaysValidPolicy()
    {
        var mock = new Mock<IWorkerSecurityPolicy>();
        mock.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkerEventHmac>()))
            .Returns(true);
        return mock.Object;
    }

    private static WorkerEventHmac ValidHmac() =>
        WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;

    private static CuttingExecution InProgressExecution(int total, int panelsCompleted, ScheduleWindow? window = null)
    {
        var tenantId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var w = window ?? ScheduleWindow.Create(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1)).Value;
        var execution = CuttingExecution.Schedule(sheetId, ValidWorker(), "M-01", w, total, tenantId).Value;
        execution.Start(execution.WorkerAssignment.WorkerId, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);

        for (var i = 1; i <= panelsCompleted; i++)
        {
            execution.RecordProgress(Guid.NewGuid(), ProgressEventKind.PanelCompleted, i, DateTime.UtcNow, ValidHmac(), AlwaysValidPolicy(), DateTime.UtcNow);
        }

        return execution;
    }

    // ── PanelCompletionPredicate ───────────────────────────────────────────────

    [Fact]
    public void PanelCompletionPredicate_AllDone_ReturnsTrue()
    {
        var execution = InProgressExecution(total: 3, panelsCompleted: 3);
        var predicate = new PanelCompletionPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public void PanelCompletionPredicate_NotAllDone_ReturnsFalse()
    {
        var execution = InProgressExecution(total: 3, panelsCompleted: 2);
        var predicate = new PanelCompletionPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void PanelCompletionPredicate_ZeroPanels_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();
        var execution = CuttingExecution.Schedule(Guid.NewGuid(), ValidWorker(), "M-01",
            ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value, 1, tenantId).Value;
        // TotalPanels = 1 but no panels completed
        var predicate = new PanelCompletionPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void PanelCompletionPredicate_Kind_IsPanelCompletion()
    {
        new PanelCompletionPredicate().Kind.Should().Be(MilestoneKind.PanelCompletion);
    }

    // ── TimeWindowPredicate ────────────────────────────────────────────────────

    [Fact]
    public void TimeWindowPredicate_WithinWindow_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var window = ScheduleWindow.Create(now.AddHours(-1), now.AddHours(1)).Value;
        var execution = CuttingExecution.Schedule(Guid.NewGuid(), ValidWorker(), "M-01", window, 1, Guid.NewGuid()).Value;
        var predicate = new TimeWindowPredicate();

        var result = predicate.Evaluate(execution, now);

        result.Should().BeTrue();
    }

    [Fact]
    public void TimeWindowPredicate_AfterWindow_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var window = ScheduleWindow.Create(now.AddHours(-3), now.AddHours(-1)).Value;
        var execution = CuttingExecution.Schedule(Guid.NewGuid(), ValidWorker(), "M-01", window, 1, Guid.NewGuid()).Value;
        var predicate = new TimeWindowPredicate();

        var result = predicate.Evaluate(execution, now);

        result.Should().BeFalse();
    }

    [Fact]
    public void TimeWindowPredicate_BeforeWindow_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var window = ScheduleWindow.Create(now.AddHours(1), now.AddHours(3)).Value;
        var execution = CuttingExecution.Schedule(Guid.NewGuid(), ValidWorker(), "M-01", window, 1, Guid.NewGuid()).Value;
        var predicate = new TimeWindowPredicate();

        var result = predicate.Evaluate(execution, now);

        result.Should().BeFalse();
    }

    [Fact]
    public void TimeWindowPredicate_Kind_IsTimeWindow()
    {
        new TimeWindowPredicate().Kind.Should().Be(MilestoneKind.TimeWindow);
    }

    // ── QualityCheckPredicate ──────────────────────────────────────────────────

    [Fact]
    public void QualityCheckPredicate_LowOffcutRatio_ReturnsTrue()
    {
        var execution = InProgressExecution(total: 3, panelsCompleted: 0);
        execution.SetTotalAreaMm2(100_000m);
        var offcut = OffcutEvent.Create(Guid.NewGuid(), 100m, 100m).Value; // 10,000 mm² = 10%
        execution.RecordOffcut(offcut, DateTime.UtcNow);
        var predicate = new QualityCheckPredicate(maxOffcutRatio: 0.30m);

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public void QualityCheckPredicate_HighOffcutRatio_ReturnsFalse()
    {
        var execution = InProgressExecution(total: 3, panelsCompleted: 0);
        execution.SetTotalAreaMm2(100_000m);
        var offcut = OffcutEvent.Create(Guid.NewGuid(), 700m, 100m).Value; // 70,000 mm² = 70%
        execution.RecordOffcut(offcut, DateTime.UtcNow);
        var predicate = new QualityCheckPredicate(maxOffcutRatio: 0.30m);

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void QualityCheckPredicate_ZeroTotalArea_ReturnsTrue()
    {
        var execution = InProgressExecution(total: 1, panelsCompleted: 0);
        var predicate = new QualityCheckPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public void QualityCheckPredicate_Kind_IsQualityCheck()
    {
        new QualityCheckPredicate().Kind.Should().Be(MilestoneKind.QualityCheck);
    }

    // ── WorkerConsentPredicate ─────────────────────────────────────────────────

    [Fact]
    public void WorkerConsentPredicate_ConsentActive_ReturnsTrue()
    {
        var execution = InProgressExecution(total: 1, panelsCompleted: 0);
        var predicate = new WorkerConsentPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeTrue();
    }

    [Fact]
    public void WorkerConsentPredicate_ConsentWithdrawn_ReturnsFalse()
    {
        var execution = InProgressExecution(total: 1, panelsCompleted: 0);
        execution.WithdrawWorkerConsent();
        var predicate = new WorkerConsentPredicate();

        var result = predicate.Evaluate(execution, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    // ── PredicateFactoryV1 ─────────────────────────────────────────────────────

    [Fact]
    public void PredicateFactoryV1_CreatesPanelCompletion_ByKind()
    {
        var factory = new PredicateFactoryV1();

        var predicate = factory.Create(MilestoneKind.PanelCompletion, "{}", 1);

        predicate.Kind.Should().Be(MilestoneKind.PanelCompletion);
    }

    [Fact]
    public void PredicateFactoryV1_CreatesTimeWindow_ByKind()
    {
        var factory = new PredicateFactoryV1();

        var predicate = factory.Create(MilestoneKind.TimeWindow, "{}", 1);

        predicate.Kind.Should().Be(MilestoneKind.TimeWindow);
    }

    [Fact]
    public void PredicateFactoryV1_CreatesQualityCheck_WithCustomRatio()
    {
        var factory = new PredicateFactoryV1();

        var predicate = factory.Create(MilestoneKind.QualityCheck, """{"maxOffcutRatio": 0.15}""", 1);

        predicate.Should().BeOfType<QualityCheckPredicate>();
    }

    [Fact]
    public void PredicateFactoryV1_CreatesWorkerConsent_ByKind()
    {
        var factory = new PredicateFactoryV1();

        var predicate = factory.Create(MilestoneKind.WorkerConsent, "{}", 1);

        predicate.Kind.Should().Be(MilestoneKind.WorkerConsent);
    }

    // ── EvaluateMilestones integration ─────────────────────────────────────────

    [Fact]
    public void EvaluateMilestones_MetPredicate_RaisesMilestoneReachedEvent()
    {
        var execution = InProgressExecution(total: 1, panelsCompleted: 1);
        execution.AddMilestone(Guid.NewGuid(), MilestoneKind.PanelCompletion, "{}", 1);
        execution.PopDomainEvents();

        execution.EvaluateMilestones(new IMilestonePredicate[] { new PanelCompletionPredicate() }, DateTime.UtcNow);

        var events = execution.PopDomainEvents();
        events.Should().ContainSingle(e => e is MilestoneReached);
    }
}
