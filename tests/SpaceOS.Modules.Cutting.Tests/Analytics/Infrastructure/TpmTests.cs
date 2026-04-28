using FluentAssertions;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.Tpm;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Infrastructure;

public class TpmTests
{
    // ══════════════════════════════════════════════════════════════════
    //  KekFallbackProvisioner (8 tests)
    // ══════════════════════════════════════════════════════════════════

    // 1. TpmEnabled=false → TryProvision returns null
    [Fact]
    public async Task TryProvisionKeyAsync_TpmDisabled_ReturnsNull()
    {
        var provisioner = new KekFallbackProvisioner(new TpmFallbackPolicy { TpmEnabled = false });
        var result = await provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.NewGuid(), default);
        result.Should().BeNull();
    }

    // 2. TpmEnabled=true → throws NotSupportedException
    [Fact]
    public async Task TryProvisionKeyAsync_TpmEnabled_ThrowsNotSupportedException()
    {
        var provisioner = new KekFallbackProvisioner(new TpmFallbackPolicy { TpmEnabled = true });
        var act = async () => await provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.NewGuid(), default);
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    // 3. IsTpmAvailable → false
    [Fact]
    public void IsTpmAvailable_AlwaysFalse()
    {
        var provisioner = new KekFallbackProvisioner(TpmFallbackPolicy.Default);
        provisioner.IsTpmAvailable.Should().BeFalse();
    }

    // 4. TpmFallbackPolicy.Default.TpmEnabled → false
    [Fact]
    public void TpmFallbackPolicy_Default_TpmEnabledIsFalse()
    {
        TpmFallbackPolicy.Default.TpmEnabled.Should().BeFalse();
    }

    // 5. TpmFallbackPolicy init syntax works
    [Fact]
    public void TpmFallbackPolicy_InitSyntax_SetsProperties()
    {
        var policy = new TpmFallbackPolicy { TpmEnabled = true };
        policy.TpmEnabled.Should().BeTrue();
    }

    // 6. TryProvision with empty tenantId → null (stub, no guard)
    [Fact]
    public async Task TryProvisionKeyAsync_EmptyTenantId_ReturnsNull()
    {
        var provisioner = new KekFallbackProvisioner(TpmFallbackPolicy.Default);
        var result = await provisioner.TryProvisionKeyAsync(Guid.Empty, Guid.NewGuid(), default);
        result.Should().BeNull();
    }

    // 7. TryProvision with empty executionId → null
    [Fact]
    public async Task TryProvisionKeyAsync_EmptyExecutionId_ReturnsNull()
    {
        var provisioner = new KekFallbackProvisioner(TpmFallbackPolicy.Default);
        var result = await provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.Empty, default);
        result.Should().BeNull();
    }

    // 8. Multiple calls → consistent null behavior
    [Fact]
    public async Task TryProvisionKeyAsync_MultipleCalls_ConsistentlyReturnsNull()
    {
        var provisioner = new KekFallbackProvisioner(TpmFallbackPolicy.Default);
        var results = await Task.WhenAll(
            provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.NewGuid(), default),
            provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.NewGuid(), default),
            provisioner.TryProvisionKeyAsync(Guid.NewGuid(), Guid.NewGuid(), default));
        results.Should().AllSatisfy(r => r.Should().BeNull());
    }

    // ══════════════════════════════════════════════════════════════════
    //  Interface / type checks (2 tests)
    // ══════════════════════════════════════════════════════════════════

    // 9. KekFallbackProvisioner implements ITpmKeyProvisioner
    [Fact]
    public void KekFallbackProvisioner_ImplementsITpmKeyProvisioner()
    {
        var provisioner = new KekFallbackProvisioner(TpmFallbackPolicy.Default);
        provisioner.Should().BeAssignableTo<ITpmKeyProvisioner>();
    }

    // 10. RedisSentinelRateLimiter implements both rate limiter interfaces
    [Fact]
    public void RedisSentinelRateLimiter_ImplementsBothInterfaces()
    {
        var rl = new RedisSentinelRateLimiter();
        rl.Should().BeAssignableTo<IRateLimiter>();
        rl.Should().BeAssignableTo<IHandshakeRateLimiter>();
    }
}
