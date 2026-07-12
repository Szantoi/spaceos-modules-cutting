using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Workers;
using Xunit;

namespace SpaceOS.Modules.Cutting.Infrastructure.Tests.Workers;

/// <summary>
/// Tests for DaySlotAutoLockWorker background service.
/// Coverage target: 90%+
/// </summary>
public sealed class DaySlotAutoLockWorkerTests
{
    private readonly Mock<ICuttingRepository> _mockRepo = new();
    private readonly Mock<ILogger<DaySlotAutoLockWorker>> _mockLogger = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public DaySlotAutoLockWorkerTests()
    {
        // Use concrete ServiceCollection instead of mocking IServiceScopeFactory
        // This avoids Moq error: "Extension methods may not be used in setup/verification expressions"
        var services = new ServiceCollection();
        services.AddScoped<ICuttingRepository>(_ => _mockRepo.Object);
        services.AddLogging();

        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_StartAndStop_ShouldExecuteWithoutError()
    {
        // Arrange
        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Setup repo to return empty slots
        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DaySlot>());

        // Act - Start worker and cancel immediately
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None).ConfigureAwait(false); // Let it start
        cts.Cancel();

        // Assert
        await task.ConfigureAwait(false);
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task LockPastSlots_NoSlots_ShouldLogZeroLockedAndZeroErrors()
    {
        // Arrange
        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DaySlot>());

        using var cts = new CancellationTokenSource();

        // Act - Run one iteration
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0 slot(s) locked, 0 error(s)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_OnePastSlot_ShouldLockAndLog()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var planId = Guid.NewGuid();
        var slot = DaySlot.Create(planId, pastDate, 8m);

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DaySlot> { slot });

        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        slot.Status.Should().Be(DaySlotStatus.Locked);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1 slot(s) locked")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_MultiplePastSlots_ShouldLockAllAndLog()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var slot1 = DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), 8m);
        var slot2 = DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), 8m);
        var slot3 = DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.Today.AddDays(-3)), 8m);

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DaySlot> { slot1, slot2, slot3 });

        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        slot1.Status.Should().Be(DaySlotStatus.Locked);
        slot2.Status.Should().Be(DaySlotStatus.Locked);
        slot3.Status.Should().Be(DaySlotStatus.Locked);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("3 slot(s) locked")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_AlreadyLockedSlot_ShouldSucceedIdempotently()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var planId = Guid.NewGuid();
        var slot = DaySlot.Create(planId, pastDate, 8m);

        // Pre-lock the slot
        var lockResult = slot.Lock();
        lockResult.IsSuccess.Should().BeTrue();
        slot.Status.Should().Be(DaySlotStatus.Locked);

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DaySlot> { slot });

        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert - Lock() is idempotent, should still succeed
        slot.Status.Should().Be(DaySlotStatus.Locked);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1 slot(s) locked")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_ClosedSlot_ShouldLogWarningAndCountError()
    {
        // Arrange
        var pastDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        var planId = Guid.NewGuid();
        var slot = DaySlot.Create(planId, pastDate, 8m);

        // Transition to Locked then Closed
        slot.Lock();
        slot.CloseSlot();
        slot.Status.Should().Be(DaySlotStatus.Closed);

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DaySlot> { slot });

        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert - Should log warning
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed to lock")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0 slot(s) locked, 1 error(s)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_MixedSuccessAndError_ShouldLogBothCounts()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var openSlot = DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), 8m);
        var closedSlot = DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.Today.AddDays(-2)), 8m);
        closedSlot.Lock();
        closedSlot.CloseSlot();

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DaySlot> { openSlot, closedSlot });

        _mockRepo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        openSlot.Status.Should().Be(DaySlotStatus.Locked);
        closedSlot.Status.Should().Be(DaySlotStatus.Closed);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("1 slot(s) locked, 1 error(s)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_RepositoryThrows_ShouldNotCrashWorker()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act - Worker should survive exception
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();

        // Assert - Should not throw
        Func<Task> act = async () => await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await act.Should().NotThrowAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task LockPastSlots_SaveChangesNotCalledWhenNoSlotsProcessed()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DaySlot>());

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert - SaveChangesAsync should NOT be called when no slots to process
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LockPastSlots_OnlyCallsGetOpenSlotsBeforeToday()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly? capturedDate = null;

        _mockRepo.Setup(r => r.GetOpenSlotsBeforeDateAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, CancellationToken>((date, ct) => capturedDate = date)
            .ReturnsAsync(Array.Empty<DaySlot>());

        var worker = new DaySlotAutoLockWorker(_scopeFactory, _mockLogger.Object);
        using var cts = new CancellationTokenSource();

        // Act
        var task = worker.StartAsync(cts.Token);
        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);

        // Assert
        capturedDate.Should().Be(today);
    }
}
