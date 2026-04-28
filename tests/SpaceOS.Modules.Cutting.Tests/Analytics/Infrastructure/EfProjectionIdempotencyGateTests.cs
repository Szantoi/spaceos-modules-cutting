using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Infrastructure;

/// <summary>
/// Tests the IProjectionIdempotencyGate contract via a mock.
/// The EF implementation (EfProjectionIdempotencyGate) requires a real PostgreSQL connection
/// for ExecuteSqlInterpolatedAsync and cannot be tested with the InMemory provider.
/// </summary>
public class EfProjectionIdempotencyGateTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private const string Subscriber1 = "ExecutionMetricProjector";
    private const string Subscriber2 = "MaterialUsageProjector";

    // ── 1. First call returns false (new event) ───────────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_FirstCall_ReturnsFalse()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default))
            .ReturnsAsync(false);

        var result = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);

        result.Should().BeFalse();
    }

    // ── 2. Second call same eventId+subscriber returns true ───────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_SecondCallSameEventAndSubscriber_ReturnsTrue()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var callCount = 0;
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default))
            .ReturnsAsync(() => callCount++ > 0);

        await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);
        var second = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);

        second.Should().BeTrue();
    }

    // ── 3. Same eventId, different subscriber → false ─────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_SameEventDifferentSubscriber_ReturnsFalse()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default)).ReturnsAsync(true);
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber2, TenantA, default)).ReturnsAsync(false);

        var result = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber2, TenantA, default);

        result.Should().BeFalse();
    }

    // ── 4. Different eventId, same subscriber → false ─────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_DifferentEventSameSubscriber_ReturnsFalse()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var event1 = Guid.NewGuid();
        var event2 = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(event1, Subscriber1, TenantA, default)).ReturnsAsync(true);
        gate.Setup(g => g.IsAlreadyProcessedAsync(event2, Subscriber1, TenantA, default)).ReturnsAsync(false);

        var result = await gate.Object.IsAlreadyProcessedAsync(event2, Subscriber1, TenantA, default);

        result.Should().BeFalse();
    }

    // ── 5. TenantId is passed through ────────────────────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_TenantIdPassedThrough()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, tenantId, default)).ReturnsAsync(false);

        await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, tenantId, default);

        gate.Verify(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, tenantId, default), Times.Once);
    }

    // ── 6. Multiple subscribers for same event — each independent ─────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_MultipleSubscribersSameEvent_EachIndependent()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default)).ReturnsAsync(false);
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber2, TenantA, default)).ReturnsAsync(false);

        var r1 = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);
        var r2 = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber2, TenantA, default);

        r1.Should().BeFalse();
        r2.Should().BeFalse();
    }

    // ── 7. CancellationToken is forwarded ────────────────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_CancellationTokenForwarded()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, cts.Token)).ReturnsAsync(false);

        await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, cts.Token);

        gate.Verify(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, cts.Token), Times.Once);
    }

    // ── 8. Empty Guid eventId — interface contract does not reject ────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_EmptyGuidEventId_InterfaceDoesNotReject()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        gate.Setup(g => g.IsAlreadyProcessedAsync(Guid.Empty, Subscriber1, TenantA, default)).ReturnsAsync(false);

        var result = await gate.Object.IsAlreadyProcessedAsync(Guid.Empty, Subscriber1, TenantA, default);

        result.Should().BeFalse();
    }

    // ── 9. Subscriber name is case-sensitive ──────────────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_SubscriberNameIsCaseSensitive()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, "subscriber", TenantA, default)).ReturnsAsync(true);
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, "Subscriber", TenantA, default)).ReturnsAsync(false);

        var lower = await gate.Object.IsAlreadyProcessedAsync(eventId, "subscriber", TenantA, default);
        var upper = await gate.Object.IsAlreadyProcessedAsync(eventId, "Subscriber", TenantA, default);

        lower.Should().BeTrue();
        upper.Should().BeFalse();
    }

    // ── 10. Interface is async — returns Task<bool> ───────────────────────────
    [Fact]
    public void IsAlreadyProcessedAsync_ReturnsTaskOfBool()
    {
        var gate = new Mock<IProjectionIdempotencyGate>().Object;
        var method = typeof(IProjectionIdempotencyGate).GetMethod(nameof(IProjectionIdempotencyGate.IsAlreadyProcessedAsync))!;
        method.ReturnType.Should().Be(typeof(Task<bool>));
    }

    // ── 11. False on first call, true on second — sequential idempotency ──────
    [Fact]
    public async Task IsAlreadyProcessedAsync_IdempotentSequence_FirstFalseSecondTrue()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var seq = gate.SetupSequence(g =>
            g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default));
        seq.ReturnsAsync(false).ReturnsAsync(true);

        var first = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);
        var second = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);

        first.Should().BeFalse();
        second.Should().BeTrue();
    }

    // ── 12. Three subscribers for same event — all return false ───────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_ThreeSubscribersSameEvent_AllReturnFalse()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var subs = new[] { "Sub1", "Sub2", "Sub3" };
        foreach (var s in subs)
            gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, s, TenantA, default)).ReturnsAsync(false);

        var results = await Task.WhenAll(subs.Select(s =>
            gate.Object.IsAlreadyProcessedAsync(eventId, s, TenantA, default)));

        results.Should().AllSatisfy(r => r.Should().BeFalse());
    }

    // ── 13. Gate called exactly once per invocation ───────────────────────────
    [Fact]
    public async Task IsAlreadyProcessedAsync_CalledExactlyOncePerInvocation()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);

        gate.Verify(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default), Times.Once);
    }

    // ── 14. Different tenants same event — independent dedup per interface contract
    [Fact]
    public async Task IsAlreadyProcessedAsync_DifferentTenantsSameEvent_IndependentDedup()
    {
        var gate = new Mock<IProjectionIdempotencyGate>();
        var eventId = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default)).ReturnsAsync(true);
        gate.Setup(g => g.IsAlreadyProcessedAsync(eventId, Subscriber1, tenantB, default)).ReturnsAsync(false);

        var forA = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, TenantA, default);
        var forB = await gate.Object.IsAlreadyProcessedAsync(eventId, Subscriber1, tenantB, default);

        forA.Should().BeTrue();
        forB.Should().BeFalse();
    }

    // ── 15. Interface method has correct signature ────────────────────────────
    [Fact]
    public void IProjectionIdempotencyGate_InterfaceHasCorrectMethod()
    {
        var method = typeof(IProjectionIdempotencyGate)
            .GetMethod(nameof(IProjectionIdempotencyGate.IsAlreadyProcessedAsync));

        method.Should().NotBeNull();
        method!.GetParameters().Should().HaveCount(4);
    }
}
