using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Queries.GetCuttingPlan;
using SpaceOS.Modules.Cutting.Application.Strategies;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateCuttingPlan;

public sealed class CreateCuttingPlanCommandHandler
    : IRequestHandler<CreateCuttingPlanCommand, Result<CreateCuttingPlanResponse>>
{
    private readonly ICuttingRepository _repository;
    private readonly IPlanningStrategyFactory _strategyFactory;
    private readonly ICapacityModel _capacityModel;

    public CreateCuttingPlanCommandHandler(
        ICuttingRepository repository,
        IPlanningStrategyFactory strategyFactory,
        ICapacityModel capacityModel)
    {
        _repository = repository;
        _strategyFactory = strategyFactory;
        _capacityModel = capacityModel;
    }

    public async Task<Result<CreateCuttingPlanResponse>> Handle(
        CreateCuttingPlanCommand request,
        CancellationToken ct)
    {
        // 1. Resolve strategy (before domain creation to fail fast on unknown id)
        IPlanningStrategy strategy;
        try
        {
            strategy = _strategyFactory.GetStrategy(request.StrategyId);
        }
        catch (ArgumentException ex)
        {
            return Result<CreateCuttingPlanResponse>.Invalid(new ValidationError(ex.Message));
        }

        // 2. Create domain aggregate (generates DaySlots internally)
        CuttingPlan plan;
        try
        {
            plan = CuttingPlan.Create(request.TenantId, request.PlanDate, request.PlanDays, request.StrategyId);
        }
        catch (ArgumentException ex)
        {
            return Result<CreateCuttingPlanResponse>.Invalid(new ValidationError(ex.Message));
        }

        // 3. Strategy-level validation
        var validation = await strategy.ValidateAsync(plan, ct).ConfigureAwait(false);
        if (!validation.IsValid)
            return Result<CreateCuttingPlanResponse>.Invalid(
                validation.Errors.Select(e => new ValidationError(e)).ToArray());

        // 4. Generate v1 seed jobs — one job per day at 91% of 8h capacity
        var seedJobs = CreateSeedJobs(plan);

        // 5. Schedule jobs via strategy
        var scheduledJobs = (await strategy
            .ScheduleJobsAsync(seedJobs, plan.DaySlots, ct)
            .ConfigureAwait(false))
            .ToList();

        // 6. Attach scheduled jobs to their day slots so CalculateYield reflects allocation
        foreach (var job in scheduledJobs)
        {
            var slot = plan.DaySlots.First(d => d.Id == job.DaySlotId);
            var addResult = slot.AddJob(job, _capacityModel);
            if (!addResult.IsSuccess)
                return Result<CreateCuttingPlanResponse>.Error("Failed to add job to slot.");
        }

        // 7. Compute yield (now that DaySlot.UsedCapacityHours is populated)
        var yield = strategy.CalculateYield(plan, plan.DaySlots);

        // 8. Persist
        await _repository.AddCuttingPlanAsync(plan, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        // 9. Build response
        var dailyPlanResponses = plan.DaySlots.Select(d => new DailyPlanResponse(
            d.Id,
            d.SlotDate.ToString("yyyy-MM-dd"),
            d.CapacityHours,
            d.UsedCapacityHours,
            d.UtilizationPercent,
            d.Jobs.Select(j => new CuttingJobResponse(
                j.Id,
                j.OrderId,
                j.ScheduledDate.ToString("yyyy-MM-dd"),
                j.Priority,
                j.EstimatedTimeHours,
                j.Status)).ToList()
        )).ToList();

        var jobResponses = scheduledJobs.Select(j => new CuttingJobResponse(
            j.Id,
            j.OrderId,
            j.ScheduledDate.ToString("yyyy-MM-dd"),
            j.Priority,
            j.EstimatedTimeHours,
            j.Status)).ToList();

        var response = new CreateCuttingPlanResponse(plan.Id, dailyPlanResponses, jobResponses, yield);
        return Result<CreateCuttingPlanResponse>.Success(response);
    }

    /// <summary>
    /// Creates representative seed jobs for v1 — one per day at 7.28h (91% of 8h capacity).
    /// These are unscheduled placeholders; real job ingestion is deferred to v1.5.
    /// </summary>
    private static IReadOnlyList<CuttingJob> CreateSeedJobs(CuttingPlan plan)
    {
        const decimal SeedHours = 7.28m; // 91% of 8h → target yield ≥ 91%

        // DaySlotId is Guid.Empty for unscheduled seeds — the strategy assigns real slot IDs.
        return plan.DaySlots
            .Select(d => CuttingJob.Create(
                daySlotId: Guid.Empty,
                orderId: Guid.NewGuid(),
                scheduledDate: d.SlotDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                priority: "Normal",
                estimatedTimeHours: SeedHours))
            .ToList();
    }
}
