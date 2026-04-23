using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Infrastructure.Workers;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure.Workers;

public class DaySlotAutoLockWorkerTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);
    private static readonly DateOnly Yesterday = Today.AddDays(-1);
    private static readonly DateOnly Tomorrow = Today.AddDays(1);

    private static DaySlot OpenSlot(DateOnly date)
        => DaySlot.Create(Guid.NewGuid(), date);

    private static (DaySlotAutoLockWorker Worker, Mock<ICuttingRepository> RepoMock) BuildWorker(
        IReadOnlyList<DaySlot> slots)
    {
        var repoMock = new Mock<ICuttingRepository>();
        repoMock
            .Setup(r => r.GetOpenSlotsBeforeDateAsync(Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);
        repoMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(repoMock.Object);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        var worker = new DaySlotAutoLockWorker(
            scopeFactory,
            NullLogger<DaySlotAutoLockWorker>.Instance);

        return (worker, repoMock);
    }

    [Fact]
    public async Task LockPastSlots_WhenPastOpenSlotsExist_ShouldLockThemAndSave()
    {
        var pastSlot = OpenSlot(Yesterday);
        var (worker, repoMock) = BuildWorker([pastSlot]);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        pastSlot.Status.Should().Be(DaySlotStatus.Locked, "past open slots must be auto-locked");
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_WhenNoPastOpenSlots_ShouldNotSave()
    {
        var (worker, repoMock) = BuildWorker([]);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "SaveChanges must not be called when there are no slots to lock");
    }

    [Fact]
    public async Task LockPastSlots_WhenSlotAlreadyLocked_ShouldRemainLockedAndNotError()
    {
        var slot = OpenSlot(Yesterday);
        slot.Lock(); // pre-lock — status is already Locked

        var (worker, repoMock) = BuildWorker([slot]);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        // Lock() is idempotent: second call returns Success → SaveChanges is called
        slot.Status.Should().Be(DaySlotStatus.Locked);
        repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LockPastSlots_WhenSlotIsClosed_ShouldNotLockIt()
    {
        var slot = OpenSlot(Yesterday);
        slot.Lock();
        slot.CloseSlot(); // now Closed

        var (worker, repoMock) = BuildWorker([slot]);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        slot.Status.Should().Be(DaySlotStatus.Closed,
            "Closed slots are not modified by the auto-lock worker");
    }

    [Fact]
    public async Task LockPastSlots_RepositoryOnlyQueriesPastSlots_FutureAndTodaySlotsNotInQuery()
    {
        // The worker calls GetOpenSlotsBeforeDateAsync(today) — future slots are never returned.
        // This test confirms the query predicate contract: repo must NOT return today/future slots.
        var (worker, repoMock) = BuildWorker([]);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(50);
        await worker.StopAsync(CancellationToken.None);

        // Verify the boundary: query must be for strictly-before-today
        repoMock.Verify(r => r.GetOpenSlotsBeforeDateAsync(
            Today,
            It.IsAny<CancellationToken>()), Times.AtLeastOnce,
            "worker must query with DateTime.Today as the exclusive upper boundary");
    }
}
