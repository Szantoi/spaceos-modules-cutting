using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Services;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application.Strategies;

/// <summary>
/// Integration tests for <see cref="CreateCuttingPlanCommandHandler"/> using the real strategy pipeline.
/// Uses InMemory EF Core — no Testcontainers required for this layer.
/// </summary>
public class CreateCuttingPlanStrategyTests : IDisposable
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTime TodayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

    private readonly CuttingDbContext _db;
    private readonly CuttingRepository _repo;
    private readonly PlanningStrategyFactory _factory;

    public CreateCuttingPlanStrategyTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new CuttingRepository(_db);
        _factory = new PlanningStrategyFactory();
    }

    private CreateCuttingPlanCommandHandler BuildHandler()
        => new(_repo, _factory, new AreaCapacityModel());

    [Fact]
    public async Task Handle_WithMaxcutV1_Returns201AndYieldAbove91Percent()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalYieldPercent.Should().BeGreaterThanOrEqualTo(91m);
    }

    [Fact]
    public async Task Handle_WithFifoStrategy_Returns201AndPositiveYield()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "fifo");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalYieldPercent.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task Handle_WithPriorityStrategy_Returns201AndPositiveYield()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "priority");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalYieldPercent.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task Handle_WithInvalidStrategy_ReturnsInvalidResult()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "bad-strategy");
        var result = await BuildHandler().Handle(command, default);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_WithMaxcutV1_ResponseContainsScheduledJobs()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.ScheduledJobs.Should().NotBeEmpty("seed jobs should be scheduled");
    }

    [Fact]
    public async Task Handle_WithMaxcutV1_ResponseContainsDailyPlans()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.DailyPlans.Should().HaveCount(7);
    }

    [Fact]
    public async Task Handle_YieldCalculationAccuracy_MaxCutAllocs91PercentOf8HSlots()
    {
        // 7 slots × 8h; each seed job = 7.28h → yield = 7.28/8 * 100 = 91%
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalYieldPercent.Should().BeApproximately(91m, 0.1m);
    }

    [Fact]
    public async Task Handle_WithCustomStrategy_FallsBackToMaxCutBehaviour()
    {
        var commandMaxCut = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var commandCustom = new CreateCuttingPlanCommand(TenantId, TodayUtc.AddDays(1), 7, "custom");

        var maxcutResult = await BuildHandler().Handle(commandMaxCut, default);
        var customResult = await BuildHandler().Handle(commandCustom, default);

        maxcutResult.IsSuccess.Should().BeTrue();
        customResult.IsSuccess.Should().BeTrue();
        customResult.Value.TotalYieldPercent.Should().BeApproximately(
            maxcutResult.Value.TotalYieldPercent, 0.01m,
            "Custom falls back to MaxCut in v1");
    }

    [Fact]
    public async Task Handle_PlanIsPersisted_CanBeRetrievedFromRepository()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        var persisted = await _repo.GetCuttingPlanByIdAsync(result.Value.PlanId);
        persisted.Should().NotBeNull();
        persisted!.StrategyId.Should().Be("maxcut-v1");
    }

    [Fact]
    public async Task Handle_WithPastPlanDate_ReturnsInvalidResult()
    {
        var pastDate = TodayUtc.AddDays(-1);
        var command = new CreateCuttingPlanCommand(TenantId, pastDate, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ResponseIncludesPlanIdDailyPlansJobsAndYield()
    {
        var command = new CreateCuttingPlanCommand(TenantId, TodayUtc, 7, "maxcut-v1");
        var result = await BuildHandler().Handle(command, default);

        result.IsSuccess.Should().BeTrue();
        result.Value.PlanId.Should().NotBeEmpty();
        result.Value.DailyPlans.Should().NotBeEmpty();
        result.Value.ScheduledJobs.Should().NotBeEmpty();
        result.Value.TotalYieldPercent.Should().BeGreaterThan(0m);
    }

    public void Dispose() => _db.Dispose();
}
