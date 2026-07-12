using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Entities;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class BatchAssignmentTests
{
    [Fact]
    public void Create_WithValidParams_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var planDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var machineId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var priority = 5;
        var startTime = DateTime.UtcNow.AddHours(1);

        // Act
        var result = BatchAssignment.Create(
            tenantId, batchId, planDate, machineId, operatorId, executionId, priority, startTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.BatchId.Should().Be(batchId);
        result.Value.PlanDate.Should().Be(planDate);
        result.Value.MachineId.Should().Be(machineId);
        result.Value.OperatorId.Should().Be(operatorId);
        result.Value.ExecutionId.Should().Be(executionId);
        result.Value.Priority.Should().Be(priority);
    }

    [Fact]
    public void Create_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange & Act
        var result = BatchAssignment.Create(
            Guid.Empty, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow.AddHours(1));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("TenantId"));
    }

    [Fact]
    public void Create_WithEmptyBatchId_ReturnsInvalid()
    {
        // Arrange & Act
        var result = BatchAssignment.Create(
            Guid.NewGuid(), Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow.AddHours(1));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("BatchId"));
    }

    [Fact]
    public void Create_WithPriorityOutOfRange_ReturnsInvalid()
    {
        // Arrange & Act
        var result = BatchAssignment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 11, DateTime.UtcNow.AddHours(1));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("Priority"));
    }

    [Fact]
    public void Create_WithPastStartTime_ReturnsInvalid()
    {
        // Arrange & Act
        var result = BatchAssignment.Create(
            Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5, DateTime.UtcNow.AddHours(-1));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("StartTime"));
    }
}
