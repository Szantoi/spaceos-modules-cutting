#pragma warning disable CS0618 // Phase 3 stub — tests intentionally cover the legacy CuttingExecution
using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class CuttingExecutionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid SheetId = Guid.NewGuid();

    [Fact]
    public void Plan_ShouldSetStatusPlanned()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Status.Should().Be(ExecutionStatus.Planned);
    }

    [Fact]
    public void Start_FromPlanned_ShouldTransitionToInProgress()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        exec.Status.Should().Be(ExecutionStatus.InProgress);
        exec.StartedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_FromInProgress_ShouldTransitionToCompleted()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        exec.Complete(150m);
        exec.Status.Should().Be(ExecutionStatus.Completed);
        exec.WasteAreaCm2.Should().Be(150m);
        exec.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Complete_ShouldRaiseCompletedAndWasteEvents()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        exec.Complete(100m);
        exec.DomainEvents.Should().Contain(e => e is CuttingExecutionCompletedEvent);
        exec.DomainEvents.Should().Contain(e => e is WasteRecordedEvent);
    }

    [Fact]
    public void Fail_FromInProgress_ShouldTransitionToFailed()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        exec.Fail();
        exec.Status.Should().Be(ExecutionStatus.Failed);
    }

    [Fact]
    public void Start_WhenNotPlanned_ShouldThrow()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        var act = () => exec.Start();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_WithNegativeWaste_ShouldThrow()
    {
        var exec = CuttingExecution.Plan(TenantId, SheetId, "operator1");
        exec.Start();
        var act = () => exec.Complete(-1m);
        act.Should().Throw<ArgumentException>();
    }
}
