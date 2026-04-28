using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Specifications;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution;

public class SpecificationTests
{
    private static IWorkerSecurityPolicy AlwaysValidPolicy()
    {
        var mock = new Mock<IWorkerSecurityPolicy>();
        mock.Setup(p => p.ValidateProgressEventHmac(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkerEventHmac>()))
            .Returns(true);
        return mock.Object;
    }

    private static WorkerEventHmac ValidHmac() =>
        WorkerEventHmac.Create(Convert.ToBase64String(new byte[32]), "v1").Value;

    private static CuttingExecution BuildExecution(Guid tenantId, Guid sheetId, string machine = "M-01", int total = 3)
    {
        var window = ScheduleWindow.Create(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(3)).Value;
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        return CuttingExecution.Schedule(sheetId, worker, machine, window, total, tenantId).Value;
    }

    private static List<CuttingExecution> SampleList(Guid tenantId, Guid sheetId)
    {
        var match = BuildExecution(tenantId, sheetId);
        var other = BuildExecution(Guid.NewGuid(), Guid.NewGuid());
        return [match, other];
    }

    [Fact]
    public void CuttingExecutionByIdSpec_MatchesCorrectExecution()
    {
        var exec = BuildExecution(Guid.NewGuid(), Guid.NewGuid());
        var spec = new CuttingExecutionByIdSpec(exec.Id);
        var list = new List<CuttingExecution> { exec, BuildExecution(Guid.NewGuid(), Guid.NewGuid()) };

        var result = list.AsQueryable().Where(spec.WhereExpressions.First().Filter).ToList();

        result.Should().ContainSingle(e => e.Id == exec.Id);
    }

    [Fact]
    public void ActiveExecutionsByTenantSpec_ReturnsOnlyActiveStatuses()
    {
        var tenantId = Guid.NewGuid();
        var active = BuildExecution(tenantId, Guid.NewGuid());
        var otherTenant = BuildExecution(Guid.NewGuid(), Guid.NewGuid());
        var spec = new ActiveExecutionsByTenantSpec(tenantId);

        var result = new List<CuttingExecution> { active, otherTenant }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.TenantId == tenantId);
    }

    [Fact]
    public void ExecutionsBySheetSpec_MatchesSheetAndTenant()
    {
        var tenantId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var match = BuildExecution(tenantId, sheetId);
        var wrongSheet = BuildExecution(tenantId, Guid.NewGuid());
        var spec = new ExecutionsBySheetSpec(sheetId, tenantId);

        var result = new List<CuttingExecution> { match, wrongSheet }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.SheetId == sheetId);
    }

    [Fact]
    public void ExecutionsByMachineAndDateSpec_MatchesMachineAndDate()
    {
        var tenantId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var match = BuildExecution(tenantId, Guid.NewGuid(), machine: "M-42");
        var otherMachine = BuildExecution(tenantId, Guid.NewGuid(), machine: "M-99");
        var spec = new ExecutionsByMachineAndDateSpec("M-42", today, tenantId);

        var result = new List<CuttingExecution> { match, otherMachine }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.MachineId == "M-42");
    }

    [Fact]
    public void PendingMilestonesSpec_MatchesExecutionWithPendingMilestone()
    {
        var tenantId = Guid.NewGuid();
        var withMilestone = BuildExecution(tenantId, Guid.NewGuid());
        withMilestone.AddMilestone(Guid.NewGuid(), MilestoneKind.PanelCompletion, "{}", 1);
        var withoutMilestone = BuildExecution(tenantId, Guid.NewGuid());
        var spec = new PendingMilestonesSpec(tenantId);

        var result = new List<CuttingExecution> { withMilestone, withoutMilestone }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.Id == withMilestone.Id);
    }

    [Fact]
    public void ExecutionsByConsentScopeSpec_MatchesWorkerAndTenant()
    {
        var tenantId = Guid.NewGuid();
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        var match = CuttingExecution.Schedule(Guid.NewGuid(), worker, "M-01", window, 1, tenantId).Value;
        var other = BuildExecution(tenantId, Guid.NewGuid());
        var spec = new ExecutionsByConsentScopeSpec(worker.WorkerId, ConsentScope.AllExecutions, tenantId);

        var result = new List<CuttingExecution> { match, other }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().Contain(e => e.Id == match.Id);
    }

    [Fact]
    public void ConsentAffectedPhotoCountSpec_OnlyMatchesPhotoEvidenceProofs()
    {
        var tenantId = Guid.NewGuid();
        var worker = WorkerAssignment.Create(Guid.NewGuid(), Guid.NewGuid()).Value;
        var window = ScheduleWindow.Create(DateTime.UtcNow, DateTime.UtcNow.AddHours(1)).Value;
        var noProof = CuttingExecution.Schedule(Guid.NewGuid(), worker, "M-01", window, 1, tenantId).Value;
        var spec = new ConsentAffectedPhotoCountSpec(worker.WorkerId, tenantId);

        var result = new List<CuttingExecution> { noProof }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ExecutionKeyByExecutionSpec_MatchesByExecutionIdAndTenant()
    {
        var tenantId = Guid.NewGuid();
        var exec = BuildExecution(tenantId, Guid.NewGuid());
        var otherTenant = BuildExecution(Guid.NewGuid(), Guid.NewGuid());
        var spec = new ExecutionKeyByExecutionSpec(exec.Id, tenantId);

        var result = new List<CuttingExecution> { exec, otherTenant }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.Id == exec.Id);
    }

    [Fact]
    public void ExecutionsByHandshakeEpicSpec_MatchesBySheetIdConvention()
    {
        var tenantId = Guid.NewGuid();
        var epicId = Guid.NewGuid();
        var match = BuildExecution(tenantId, epicId);
        var other = BuildExecution(tenantId, Guid.NewGuid());
        var spec = new ExecutionsByHandshakeEpicSpec(epicId, tenantId);

        var result = new List<CuttingExecution> { match, other }
            .AsQueryable()
            .Where(spec.WhereExpressions.First().Filter)
            .ToList();

        result.Should().ContainSingle(e => e.SheetId == epicId);
    }
}
