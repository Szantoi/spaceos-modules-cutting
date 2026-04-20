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

    public CreateCuttingPlanCommandHandler(
        ICuttingRepository repository,
        IPlanningStrategyFactory strategyFactory)
    {
        _repository = repository;
        _strategyFactory = strategyFactory;
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

        // 2. Create domain aggregate (generates DailyPlans internally)
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
        //    (real jobs come from order aggregates in v1.5+)
        var seedJobs = CreateSeedJobs(plan);

        // 5. Schedule jobs via strategy
        var scheduledJobs = (await strategy
            .ScheduleJobsAsync(seedJobs, plan.DailyPlans, ct)
            .ConfigureAwait(false))
            .ToList();

        // 6. Attach scheduled jobs to their daily plan slots so CalculateYield reflects allocation
        foreach (var job in scheduledJobs)
        {
            var slot = plan.DailyPlans.First(d => d.Id == job.DailyPlanId);
            slot.AddJob(job);
        }

        // 7. Compute yield (now that DailyPlan.AllocatedCapacity is populated)
        var yield = strategy.CalculateYield(plan, plan.DailyPlans);

        // 8. Persist
        await _repository.AddCuttingPlanAsync(plan, ct).ConfigureAwait(false);
        await _repository.SaveChangesAsync(ct).ConfigureAwait(false);

        // 9. Build response
        var dailyPlanResponses = plan.DailyPlans.Select(d => new DailyPlanResponse(
            d.Id,
            d.Date.ToString("yyyy-MM-dd"),
            d.AvailableCapacity,
            d.AllocatedCapacity,
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

        // DailyPlanId is Guid.Empty for unscheduled seeds — the strategy assigns real slot IDs.
        return plan.DailyPlans
            .Select(d => CuttingJob.Create(
                dailyPlanId: Guid.Empty,
                orderId: Guid.NewGuid(),
                scheduledDate: d.Date,
                priority: "Normal",
                estimatedTimeHours: SeedHours))
            .ToList();
    }
}
