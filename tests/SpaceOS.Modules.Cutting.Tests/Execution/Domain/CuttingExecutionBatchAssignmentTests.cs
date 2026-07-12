using Ardalis.Result;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Domain;

/// <summary>
/// Tests for CuttingExecution aggregate with batch assignment (TOP 3 FE dependency).
/// </summary>
public sealed class CuttingExecutionBatchAssignmentTests
{
    [Fact]
    public void ScheduleWithBatchAssignment_ValidParameters_TransitionsToScheduledState()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var machineId = "MACHINE-01";
        var tenantId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(8);

        var workerAssignment = WorkerAssignment.Create(workerId, enrollmentId).Value;
        var scheduleWindow = ScheduleWindow.Create(start, end).Value;

        // Act
        var result = CuttingExecution.ScheduleWithBatchAssignment(
            batchId: batchId,
            sheetId: sheetId,
            workerAssignment: workerAssignment,
            machineId: machineId,
            scheduleWindow: scheduleWindow,
            totalPanels: 10,
            priority: 5,
            tenantId: tenantId);

        // Assert
        Assert.True(result.IsSuccess);
        var execution = result.Value;
        Assert.Equal(CuttingExecutionStatus.Scheduled, execution.Status);
        Assert.Equal(batchId, execution.BatchId);
        Assert.Equal(sheetId, execution.SheetId);
        Assert.Equal(machineId, execution.MachineId);
        Assert.Equal(10, execution.TotalPanels);
        Assert.Equal(5, execution.Priority);
        Assert.Equal(tenantId, execution.TenantId);
        Assert.NotEqual(Guid.Empty, execution.Id);
    }

    [Fact]
    public void ScheduleWithBatchAssignment_EmptyBatchId_ReturnsInvalid()
    {
        // Arrange
        var sheetId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var machineId = "MACHINE-01";
        var tenantId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(8);

        var workerAssignment = WorkerAssignment.Create(workerId, enrollmentId).Value;
        var scheduleWindow = ScheduleWindow.Create(start, end).Value;

        // Act
        var result = CuttingExecution.ScheduleWithBatchAssignment(
            batchId: Guid.Empty, // Invalid
            sheetId: sheetId,
            workerAssignment: workerAssignment,
            machineId: machineId,
            scheduleWindow: scheduleWindow,
            totalPanels: 10,
            priority: 5,
            tenantId: tenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("BatchId", result.ValidationErrors.First().ErrorMessage);
    }

    [Fact]
    public void ScheduleWithBatchAssignment_InvalidPriority_ReturnsInvalid()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var machineId = "MACHINE-01";
        var tenantId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(8);

        var workerAssignment = WorkerAssignment.Create(workerId, enrollmentId).Value;
        var scheduleWindow = ScheduleWindow.Create(start, end).Value;

        // Act
        var result = CuttingExecution.ScheduleWithBatchAssignment(
            batchId: batchId,
            sheetId: sheetId,
            workerAssignment: workerAssignment,
            machineId: machineId,
            scheduleWindow: scheduleWindow,
            totalPanels: 10,
            priority: 15, // Invalid: must be 1-10
            tenantId: tenantId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("Priority", result.ValidationErrors.First().ErrorMessage);
    }

    [Fact]
    public void ScheduleWithBatchAssignment_Priority1To10_AllValid()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var sheetId = Guid.NewGuid();
        var workerId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var machineId = "MACHINE-01";
        var tenantId = Guid.NewGuid();
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(8);

        var workerAssignment = WorkerAssignment.Create(workerId, enrollmentId).Value;
        var scheduleWindow = ScheduleWindow.Create(start, end).Value;

        // Act & Assert - Test all valid priorities (1-10)
        for (int priority = 1; priority <= 10; priority++)
        {
            var result = CuttingExecution.ScheduleWithBatchAssignment(
                batchId: batchId,
                sheetId: sheetId,
                workerAssignment: workerAssignment,
                machineId: machineId,
                scheduleWindow: scheduleWindow,
                totalPanels: 10,
                priority: priority,
                tenantId: tenantId);

            Assert.True(result.IsSuccess, $"Priority {priority} should be valid");
            Assert.Equal(priority, result.Value.Priority);
        }
    }
}
