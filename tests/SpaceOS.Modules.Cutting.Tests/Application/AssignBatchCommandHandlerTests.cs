using Ardalis.Result;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.AssignBatch;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Execution.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Execution.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

/// <summary>
/// Tests for the AssignBatchCommandHandler (TOP 3 FE dependency endpoint).
/// </summary>
public sealed class AssignBatchCommandHandlerTests
{
    private readonly Mock<ICuttingRepository> _mockCuttingRepo;
    private readonly Mock<ICuttingExecutionRepository> _mockExecutionRepo;
    private readonly AssignBatchCommandHandler _handler;

    public AssignBatchCommandHandlerTests()
    {
        _mockCuttingRepo = new Mock<ICuttingRepository>();
        _mockExecutionRepo = new Mock<ICuttingExecutionRepository>();
        _handler = new AssignBatchCommandHandler(_mockCuttingRepo.Object, _mockExecutionRepo.Object);
    }

    [Fact]
    public async Task AssignBatch_ValidRequest_ReturnsExecutionId()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var machineId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var planDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var startTime = DateTime.UtcNow.AddHours(2);

        var mockBatch = CuttingBatch.Create(
            planId: Guid.NewGuid(),
            materialType: "MDF",
            thicknessMm: 18m,
            sheetIds: new[] { Guid.NewGuid() });

        _mockCuttingRepo
            .Setup(r => r.GetCuttingBatchByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockBatch);

        _mockCuttingRepo
            .Setup(r => r.GetBatchAssignmentAsync(batchId, planDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchAssignment?)null);

        _mockCuttingRepo
            .Setup(r => r.AddBatchAssignmentAsync(It.IsAny<BatchAssignment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockCuttingRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockExecutionRepo
            .Setup(r => r.AddAsync(It.IsAny<CuttingExecution>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new AssignBatchCommand(
            TenantId: tenantId,
            PlanDate: planDate,
            BatchId: batchId,
            MachineId: machineId,
            OperatorId: operatorId,
            Priority: 5,
            StartTime: startTime);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.ExecutionId);
        Assert.Equal("Planned", result.Value.Status);

        _mockExecutionRepo.Verify(r => r.AddAsync(
            It.Is<CuttingExecution>(e =>
                e.TenantId == tenantId &&
                e.Status == SpaceOS.Modules.Cutting.Execution.Domain.Enums.CuttingExecutionStatus.Scheduled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignBatch_BatchNotFound_ReturnsInvalid()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        _mockCuttingRepo
            .Setup(r => r.GetCuttingBatchByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuttingBatch?)null);

        var command = new AssignBatchCommand(
            TenantId: Guid.NewGuid(),
            PlanDate: DateOnly.FromDateTime(DateTime.UtcNow),
            BatchId: batchId,
            MachineId: Guid.NewGuid(),
            OperatorId: Guid.NewGuid(),
            Priority: 5,
            StartTime: DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("not found", result.ValidationErrors.First().ErrorMessage);
    }

    [Fact]
    public async Task AssignBatch_DuplicateBatch_Returns409Conflict()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var planDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var mockBatch = CuttingBatch.Create(
            planId: Guid.NewGuid(),
            materialType: "MDF",
            thicknessMm: 18m,
            sheetIds: new[] { Guid.NewGuid() });

        var existingAssignment = BatchAssignment.Create(
            tenantId: tenantId,
            batchId: batchId,
            planDate: planDate,
            machineId: Guid.NewGuid(),
            operatorId: Guid.NewGuid(),
            executionId: Guid.NewGuid(),
            priority: 5,
            startTime: DateTime.UtcNow.AddHours(1)).Value;

        _mockCuttingRepo
            .Setup(r => r.GetCuttingBatchByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockBatch);

        _mockCuttingRepo
            .Setup(r => r.GetBatchAssignmentAsync(batchId, planDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);

        var command = new AssignBatchCommand(
            TenantId: tenantId,
            PlanDate: planDate,
            BatchId: batchId,
            MachineId: Guid.NewGuid(),
            OperatorId: Guid.NewGuid(),
            Priority: 5,
            StartTime: DateTime.UtcNow.AddHours(2));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Contains("already assigned", string.Join(" ", result.Errors));
    }

    [Fact]
    public async Task AssignBatch_InvalidPriority_ReturnsInvalid()
    {
        // Arrange
        var batchId = Guid.NewGuid();
        var mockBatch = CuttingBatch.Create(
            planId: Guid.NewGuid(),
            materialType: "MDF",
            thicknessMm: 18m,
            sheetIds: new[] { Guid.NewGuid() });

        _mockCuttingRepo
            .Setup(r => r.GetCuttingBatchByIdAsync(batchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockBatch);

        _mockCuttingRepo
            .Setup(r => r.GetBatchAssignmentAsync(batchId, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchAssignment?)null);

        var command = new AssignBatchCommand(
            TenantId: Guid.NewGuid(),
            PlanDate: DateOnly.FromDateTime(DateTime.UtcNow),
            BatchId: batchId,
            MachineId: Guid.NewGuid(),
            OperatorId: Guid.NewGuid(),
            Priority: 15, // Invalid: must be 1-10
            StartTime: DateTime.UtcNow.AddHours(1));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains("Priority", result.ValidationErrors.First().ErrorMessage);
    }
}
