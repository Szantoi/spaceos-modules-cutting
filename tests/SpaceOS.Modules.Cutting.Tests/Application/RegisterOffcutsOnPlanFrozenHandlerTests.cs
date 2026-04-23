using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Application.EventHandlers;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using System.Text.Json;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class RegisterOffcutsOnPlanFrozenHandlerTests
{
    private readonly Mock<IPlanNestingSnapshotRepository> _snapshotRepoMock = new();
    private readonly Mock<IInventoryCuttingAdapter> _adapterMock = new();

    private RegisterOffcutsOnPlanFrozenHandler CreateHandler() => new(
        _snapshotRepoMock.Object,
        _adapterMock.Object,
        NullLogger<RegisterOffcutsOnPlanFrozenHandler>.Instance);

    private static string BuildSnapshotJson(
        string materialCode = "MDF18mm",
        decimal width = 600m, decimal height = 500m,
        decimal x = 0m, decimal y = 0m)
    {
        var payload = new[]
        {
            new
            {
                MaterialCode = materialCode,
                WastePieces = new[]
                {
                    new { X = x, Y = y, WidthMm = width, HeightMm = height }
                }
            }
        };
        return JsonSerializer.Serialize(payload);
    }

    // ── 1. Snapshot missing → LogWarning, no exception ────────────────────────

    [Fact]
    public async Task Handle_SnapshotMissing_LogsWarning_NoException()
    {
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((PlanNestingSnapshot?)null);

        var handler = CreateHandler();
        var notification = new CuttingPlanFrozenNotification(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Must not throw
        var act = async () => await handler.Handle(notification, default);
        await act.Should().NotThrowAsync();

        _adapterMock.Verify(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default),
            Times.Never);
    }

    // ── 2. Below 400mm threshold → not registered ─────────────────────────────

    [Fact]
    public async Task Handle_BelowThreshold_NotRegistered()
    {
        var planId = Guid.NewGuid();
        var json = BuildSnapshotJson(width: 300m, height: 300m); // below 400mm threshold
        var snapshot = PlanNestingSnapshot.Create(planId, Guid.NewGuid(), json);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default)).ReturnsAsync(snapshot);

        var handler = CreateHandler();
        await handler.Handle(new CuttingPlanFrozenNotification(planId, Guid.NewGuid(), DateTimeOffset.UtcNow), default);

        _adapterMock.Verify(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default),
            Times.Never);
    }

    // ── 3. Above threshold → RegisterOffcutsAsync called ─────────────────────

    [Fact]
    public async Task Handle_AboveThreshold_RegistersCalled()
    {
        var planId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var json = BuildSnapshotJson(width: 500m, height: 600m); // above 400mm threshold
        var snapshot = PlanNestingSnapshot.Create(planId, tenantId, json);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default)).ReturnsAsync(snapshot);
        _adapterMock.Setup(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default))
            .ReturnsAsync(Ardalis.Result.Result.Success());

        var handler = CreateHandler();
        await handler.Handle(new CuttingPlanFrozenNotification(planId, tenantId, DateTimeOffset.UtcNow), default);

        _adapterMock.Verify(a => a.RegisterOffcutsAsync(
            planId, tenantId,
            It.Is<IReadOnlyList<OffcutRegistrationItem>>(items => items.Count == 1),
            default),
            Times.Once);
    }

    // ── 4. Adapter fails → no exception (best-effort) ────────────────────────

    [Fact]
    public async Task Handle_AdapterFails_NoException()
    {
        var planId = Guid.NewGuid();
        var json = BuildSnapshotJson(width: 500m, height: 500m);
        var snapshot = PlanNestingSnapshot.Create(planId, Guid.NewGuid(), json);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default)).ReturnsAsync(snapshot);
        _adapterMock.Setup(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default))
            .ThrowsAsync(new HttpRequestException("Inventory offline"));

        var handler = CreateHandler();
        var act = async () => await handler.Handle(
            new CuttingPlanFrozenNotification(planId, Guid.NewGuid(), DateTimeOffset.UtcNow), default);

        await act.Should().NotThrowAsync("handler is best-effort");
    }

    // ── 5. Idempotent freeze → adapter called only once (snapshot read-only) ──

    [Fact]
    public async Task Handle_IdempotentFreeze_NoDuplicateOffcuts()
    {
        var planId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var json = BuildSnapshotJson(width: 500m, height: 500m);
        var snapshot = PlanNestingSnapshot.Create(planId, tenantId, json);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default)).ReturnsAsync(snapshot);
        _adapterMock.Setup(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default))
            .ReturnsAsync(Ardalis.Result.Result.Success());

        var handler = CreateHandler();
        var notification = new CuttingPlanFrozenNotification(planId, tenantId, DateTimeOffset.UtcNow);

        // Simulate two freeze events
        await handler.Handle(notification, default);
        await handler.Handle(notification, default);

        // Adapter called twice (idempotency is the Inventory's responsibility via CorrelationId=PlanId)
        _adapterMock.Verify(a => a.RegisterOffcutsAsync(
            planId, tenantId, It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default),
            Times.Exactly(2));
    }

    // ── 6. Cross-tenant: Tenant A's snapshot not shared with Tenant B ─────────

    [Fact]
    public async Task Handle_CrossTenant_SnapshotBelongsToCorrectTenant()
    {
        var planIdA = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var json = BuildSnapshotJson(width: 500m, height: 500m);
        var snapshotA = PlanNestingSnapshot.Create(planIdA, tenantA, json);

        // Plan B has no snapshot
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planIdA, default)).ReturnsAsync(snapshotA);

        var handlerA = CreateHandler();
        var handlerB = CreateHandler();

        // Tenant A freeze succeeds and registers offcuts
        _adapterMock.Setup(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default))
            .ReturnsAsync(Ardalis.Result.Result.Success());
        await handlerA.Handle(new CuttingPlanFrozenNotification(planIdA, tenantA, DateTimeOffset.UtcNow), default);

        _adapterMock.Verify(a => a.RegisterOffcutsAsync(planIdA, tenantA, It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default), Times.Once);

        // Verify adapter is NOT called with tenantB
        _adapterMock.Verify(a => a.RegisterOffcutsAsync(It.IsAny<Guid>(), tenantB, It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default), Times.Never);
    }

    // ── 7. Mixed threshold → only eligible pieces registered ─────────────────

    [Fact]
    public async Task Handle_MixedThreshold_OnlyEligiblePiecesRegistered()
    {
        var planId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var payload = new[]
        {
            new
            {
                MaterialCode = "MDF18mm",
                WastePieces = new[]
                {
                    new { X = 0m, Y = 0m, WidthMm = 500m, HeightMm = 500m }, // above threshold
                    new { X = 0m, Y = 0m, WidthMm = 200m, HeightMm = 200m }, // below threshold
                    new { X = 0m, Y = 0m, WidthMm = 400m, HeightMm = 400m }, // exactly at threshold
                }
            }
        };
        var json = JsonSerializer.Serialize(payload);
        var snapshot = PlanNestingSnapshot.Create(planId, tenantId, json);
        _snapshotRepoMock.Setup(r => r.GetByPlanAsync(planId, default)).ReturnsAsync(snapshot);
        _adapterMock.Setup(a => a.RegisterOffcutsAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<IReadOnlyList<OffcutRegistrationItem>>(), default))
            .ReturnsAsync(Ardalis.Result.Result.Success());

        var handler = CreateHandler();
        await handler.Handle(new CuttingPlanFrozenNotification(planId, tenantId, DateTimeOffset.UtcNow), default);

        // Only 2 pieces pass threshold (500x500 and exactly 400x400)
        _adapterMock.Verify(a => a.RegisterOffcutsAsync(
            planId, tenantId,
            It.Is<IReadOnlyList<OffcutRegistrationItem>>(items => items.Count == 2),
            default),
            Times.Once);
    }
}
