using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;

namespace SpaceOS.Modules.Cutting.Application.Commands.AssignBatch;

/// <summary>
/// Handles batch assignment by creating a <see cref="BatchAssignment"/> record
/// and scheduling a <see cref="CuttingExecution"/> for the batch's sheets.
/// Returns 409 Conflict if the batch is already assigned for the given date.
/// </summary>
public sealed class AssignBatchCommandHandler
    : IRequestHandler<AssignBatchCommand, Result<AssignBatchResponse>>
{
    private readonly ICuttingRepository _cuttingRepo;
    private readonly ICuttingExecutionRepository _executionRepo;

    public AssignBatchCommandHandler(
        ICuttingRepository cuttingRepo,
        ICuttingExecutionRepository executionRepo)
    {
        _cuttingRepo = cuttingRepo;
        _executionRepo = executionRepo;
    }

    public async Task<Result<AssignBatchResponse>> Handle(
        AssignBatchCommand request,
        CancellationToken ct)
    {
        // 1. Validate batch exists
        var batch = await _cuttingRepo.GetCuttingBatchByIdAsync(request.BatchId, ct)
            .ConfigureAwait(false);
        if (batch is null)
            return Result<AssignBatchResponse>.Invalid(
                new ValidationError($"Batch {request.BatchId} not found."));

        // 2. Check idempotency: is this batch already assigned for this date?
        var existingAssignment = await _cuttingRepo.GetBatchAssignmentAsync(
            request.BatchId, request.PlanDate, ct).ConfigureAwait(false);
        if (existingAssignment is not null)
            return Result<AssignBatchResponse>.Conflict(
                $"Batch {request.BatchId} is already assigned for {request.PlanDate}.");

        // 3. Validate priority range
        if (request.Priority < 1 || request.Priority > 10)
            return Result<AssignBatchResponse>.Invalid(
                new ValidationError("Priority must be between 1 and 10."));

        // 4. Validate start time is not in the past
        if (request.StartTime < DateTime.UtcNow.AddMinutes(-5))
            return Result<AssignBatchResponse>.Invalid(
                new ValidationError("StartTime cannot be in the past."));

        // 5. Create WorkerAssignment (using OperatorId as WorkerId, generating EnrollmentId)
        var enrollmentId = Guid.NewGuid(); // Enrollment ID placeholder for this batch
        var workerResult = WorkerAssignment.Create(request.OperatorId, enrollmentId);
        if (!workerResult.IsSuccess)
            return Result<AssignBatchResponse>.Invalid(workerResult.ValidationErrors);

        // 6. Create ScheduleWindow (default 8 hours)
        var scheduleEnd = request.StartTime.AddHours(8);
        var windowResult = ScheduleWindow.Create(request.StartTime, scheduleEnd);
        if (!windowResult.IsSuccess)
            return Result<AssignBatchResponse>.Invalid(windowResult.ValidationErrors);

        // 7. Create CuttingExecution with batch assignment (total panels = batch sheet count)
        var totalPanels = batch.SheetIds.Count > 0 ? batch.SheetIds.Count : 1;
        var sheetId = batch.SheetIds.Count > 0 ? batch.SheetIds[0] : request.BatchId;
        var executionResult = CuttingExecution.ScheduleWithBatchAssignment(
            batchId: request.BatchId,
            sheetId: sheetId,
            workerAssignment: workerResult.Value,
            machineId: request.MachineId.ToString(),
            scheduleWindow: windowResult.Value,
            totalPanels: totalPanels,
            priority: request.Priority,
            tenantId: request.TenantId);

        if (!executionResult.IsSuccess)
            return Result<AssignBatchResponse>.Invalid(executionResult.ValidationErrors);

        // 8. Create BatchAssignment for idempotency tracking
        var assignmentResult = BatchAssignment.Create(
            tenantId: request.TenantId,
            batchId: request.BatchId,
            planDate: request.PlanDate,
            machineId: request.MachineId,
            operatorId: request.OperatorId,
            executionId: executionResult.Value.Id,
            priority: request.Priority,
            startTime: request.StartTime);

        if (!assignmentResult.IsSuccess)
            return Result<AssignBatchResponse>.Invalid(assignmentResult.ValidationErrors);

        // 9. Persist both entities
        await _executionRepo.AddAsync(executionResult.Value, ct).ConfigureAwait(false);
        await _cuttingRepo.AddBatchAssignmentAsync(assignmentResult.Value, ct).ConfigureAwait(false);

        // Save via cutting repo (single SaveChanges for both since they share DbContext)
        await _cuttingRepo.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result<AssignBatchResponse>.Success(
            new AssignBatchResponse(executionResult.Value.Id, "Planned"));
    }
}
