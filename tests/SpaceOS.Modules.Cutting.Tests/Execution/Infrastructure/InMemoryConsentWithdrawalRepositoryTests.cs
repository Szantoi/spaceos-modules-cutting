using FluentAssertions;
using SpaceOS.Modules.Cutting.Execution.Application.Entities;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class InMemoryConsentWithdrawalRepositoryTests
{
    private readonly InMemoryConsentWithdrawalRepository _repo = new();

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsWithdrawal()
    {
        var withdrawal = ConsentWithdrawal.Create(Guid.NewGuid(), Guid.NewGuid(), ConsentScope.AllExecutions, DateTime.UtcNow);
        await _repo.SaveAsync(withdrawal, CancellationToken.None);

        var found = await _repo.GetByIdAsync(withdrawal.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Id.Should().Be(withdrawal.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NotExisting_ReturnsNull()
    {
        var found = await _repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        found.Should().BeNull();
    }

    [Fact]
    public async Task PickupNextPendingAsync_WithPendingWithdrawal_ReturnsIt()
    {
        var withdrawal = ConsentWithdrawal.Create(Guid.NewGuid(), Guid.NewGuid(), ConsentScope.AllExecutions, DateTime.UtcNow);
        await _repo.SaveAsync(withdrawal, CancellationToken.None);

        var pending = await _repo.PickupNextPendingAsync(CancellationToken.None);
        pending.Should().NotBeNull();
        pending!.Id.Should().Be(withdrawal.Id);
    }

    [Fact]
    public async Task PickupNextPendingAsync_WithNoWithdrawals_ReturnsNull()
    {
        var pending = await _repo.PickupNextPendingAsync(CancellationToken.None);
        pending.Should().BeNull();
    }

    [Fact]
    public async Task PickupNextPendingAsync_WithCompletedWithdrawal_ReturnsNull()
    {
        var withdrawal = ConsentWithdrawal.Create(Guid.NewGuid(), Guid.NewGuid(), ConsentScope.AllExecutions, DateTime.UtcNow);
        withdrawal.MarkCompleted(DateTime.UtcNow);
        await _repo.SaveAsync(withdrawal, CancellationToken.None);

        var pending = await _repo.PickupNextPendingAsync(CancellationToken.None);
        pending.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_NullWithdrawal_ThrowsArgumentNullException()
    {
        var act = async () => await _repo.SaveAsync(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ListAffectedExecutionsBatchAsync_ReturnsEmpty()
    {
        var result = await _repo.ListAffectedExecutionsBatchAsync(
            Guid.NewGuid(), Guid.NewGuid(), ConsentScope.AllExecutions, 0, 10, CancellationToken.None);
        result.Should().BeEmpty();
    }
}
