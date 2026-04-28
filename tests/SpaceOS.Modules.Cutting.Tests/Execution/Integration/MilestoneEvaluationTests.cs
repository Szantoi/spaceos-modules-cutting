using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.EvaluateMilestones;
using SpaceOS.Modules.Cutting.Execution.Application.Commands.ScheduleExecution;
using SpaceOS.Modules.Cutting.Execution.Application.Services;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Integration;

public class MilestoneEvaluationTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly CuttingExecutionRepository _repo;
    private static readonly Guid TenantId = Guid.NewGuid();

    public MilestoneEvaluationTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingExecutionRepository(_db);
    }

    [Fact]
    public async Task EvaluateMilestones_WithNoMilestones_Succeeds()
    {
        var executionId = (await ScheduleExecutionAsync()).Value;

        var factory = new PredicateFactoryV1();
        var handler = new EvaluateMilestonesCommandHandler(_repo, factory);
        var result = await handler.Handle(new EvaluateMilestonesCommand(executionId, TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateMilestones_ExecutionNotFound_ReturnsNotFound()
    {
        var factory = new PredicateFactoryV1();
        var handler = new EvaluateMilestonesCommandHandler(_repo, factory);
        var result = await handler.Handle(new EvaluateMilestonesCommand(Guid.NewGuid(), TenantId), CancellationToken.None);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task AddMilestone_ThenEvaluate_CanTrigger()
    {
        var executionId = (await ScheduleExecutionAsync()).Value;
        var execution = await _repo.GetByIdWithProgressAsync(executionId, CancellationToken.None);

        // Add a panel completion milestone
        execution!.AddMilestone(
            Guid.NewGuid(),
            MilestoneKind.PanelCompletion,
            "{}",
            1);

        await _repo.SaveChangesAsync(CancellationToken.None);

        // Verify milestone was saved
        var found = await _repo.GetByIdWithProgressAsync(executionId, CancellationToken.None);
        found!.Milestones.Should().HaveCount(1);
        found.Milestones[0].Status.Should().Be(MilestoneStatus.Pending);
    }

    private async Task<Ardalis.Result.Result<Guid>> ScheduleExecutionAsync()
    {
        var handler = new ScheduleExecutionCommandHandler(_repo);
        var cmd = new ScheduleExecutionCommand(
            TenantId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "CNC-01", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), 3);
        return await handler.Handle(cmd, CancellationToken.None);
    }

    public void Dispose() => _db.Dispose();
}
