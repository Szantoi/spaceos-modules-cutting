using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Analytics.Infrastructure.RateLimiter;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Analytics.Infrastructure;

public class RateLimiterTests
{
    // ══════════════════════════════════════════════════════════════════
    //  RedisSentinelRateLimiter (10 tests)
    // ══════════════════════════════════════════════════════════════════

    // 1. First request → true
    [Fact]
    public async Task TryAcquireAsync_FirstRequest_ReturnsTrue()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 5, window: TimeSpan.FromMinutes(1));
        var result = await rl.TryAcquireAsync("key1", default);
        result.Should().BeTrue();
    }

    // 2. Requests up to limit → all true
    [Fact]
    public async Task TryAcquireAsync_RequestsUpToLimit_AllReturnTrue()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 3, window: TimeSpan.FromMinutes(1));
        var results = new List<bool>();
        for (var i = 0; i < 3; i++)
            results.Add(await rl.TryAcquireAsync("key", default));
        results.Should().AllSatisfy(r => r.Should().BeTrue());
    }

    // 3. Request over limit → false
    [Fact]
    public async Task TryAcquireAsync_OverLimit_ReturnsFalse()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 2, window: TimeSpan.FromMinutes(1));
        await rl.TryAcquireAsync("key", default);
        await rl.TryAcquireAsync("key", default);
        var result = await rl.TryAcquireAsync("key", default);
        result.Should().BeFalse();
    }

    // 4. Different keys independent
    [Fact]
    public async Task TryAcquireAsync_DifferentKeys_Independent()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 1, window: TimeSpan.FromMinutes(1));
        await rl.TryAcquireAsync("keyA", default); // exhaust keyA
        var resultB = await rl.TryAcquireAsync("keyB", default);
        resultB.Should().BeTrue();
    }

    // 5. IHandshakeRateLimiter.TryAcquireAsync → delegates to same logic
    [Fact]
    public async Task IHandshakeRateLimiter_TryAcquireAsync_DelegatesToSameLogic()
    {
        IHandshakeRateLimiter rl = new RedisSentinelRateLimiter(maxRequests: 1, window: TimeSpan.FromMinutes(1));
        var first = await rl.TryAcquireAsync("tenant1", default);
        var second = await rl.TryAcquireAsync("tenant1", default);
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    // 6. Window reset → allows new requests after window expires
    [Fact]
    public async Task TryAcquireAsync_AfterWindowExpiry_AllowsNewRequests()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 1, window: TimeSpan.FromMilliseconds(10));
        await rl.TryAcquireAsync("key", default);
        var throttled = await rl.TryAcquireAsync("key", default);
        throttled.Should().BeFalse();

        await Task.Delay(20); // wait for window to expire
        var afterReset = await rl.TryAcquireAsync("key", default);
        afterReset.Should().BeTrue();
    }

    // 7. Default maxRequests = 60
    [Fact]
    public async Task TryAcquireAsync_DefaultMaxRequests_Allows60()
    {
        var rl = new RedisSentinelRateLimiter(); // default
        var results = new List<bool>();
        for (var i = 0; i < 60; i++)
            results.Add(await rl.TryAcquireAsync("key", default));
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        var over = await rl.TryAcquireAsync("key", default);
        over.Should().BeFalse();
    }

    // 8. Custom maxRequests constructor
    [Fact]
    public async Task TryAcquireAsync_CustomMaxRequests_Respected()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 10);
        for (var i = 0; i < 10; i++)
            await rl.TryAcquireAsync("key", default);
        var over = await rl.TryAcquireAsync("key", default);
        over.Should().BeFalse();
    }

    // 9. Custom window constructor
    [Fact]
    public async Task TryAcquireAsync_CustomWindow_Respected()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 1, window: TimeSpan.FromSeconds(60));
        await rl.TryAcquireAsync("key", default);
        var throttled = await rl.TryAcquireAsync("key", default);
        throttled.Should().BeFalse();
    }

    // 10. Same key sequential — counter is isolated per key
    [Fact]
    public async Task TryAcquireAsync_SameKeySequential_CounterIsolated()
    {
        var rl = new RedisSentinelRateLimiter(maxRequests: 2, window: TimeSpan.FromMinutes(1));
        var r1 = await rl.TryAcquireAsync("A", default);
        var r2 = await rl.TryAcquireAsync("B", default);
        var r3 = await rl.TryAcquireAsync("A", default);
        var r4 = await rl.TryAcquireAsync("A", default); // over limit for A

        r1.Should().BeTrue();
        r2.Should().BeTrue();
        r3.Should().BeTrue();
        r4.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════════════════
    //  RedisSentinelHandshakeRateLimiter (5 tests)
    // ══════════════════════════════════════════════════════════════════

#pragma warning disable CS0618 // intentionally testing the obsolete adapter
    // 11. Adapter delegates TryAcquireAsync to inner
    [Fact]
    public async Task Adapter_TryAcquireAsync_DelegatesToInner()
    {
        var inner = new RedisSentinelRateLimiter(maxRequests: 5);
        var adapter = new RedisSentinelHandshakeRateLimiter(inner);
        var result = await adapter.TryAcquireAsync("tenant1", default);
        result.Should().BeTrue();
    }

    // 12. Inner returns true → adapter returns true
    [Fact]
    public async Task Adapter_InnerReturnsTrue_AdapterReturnsTrue()
    {
        var inner = new RedisSentinelRateLimiter(maxRequests: 100);
        var adapter = new RedisSentinelHandshakeRateLimiter(inner);
        var result = await adapter.TryAcquireAsync("key", default);
        result.Should().BeTrue();
    }

    // 13. Inner returns false → adapter returns false
    [Fact]
    public async Task Adapter_InnerReturnsFalse_AdapterReturnsFalse()
    {
        var inner = new RedisSentinelRateLimiter(maxRequests: 1);
        await inner.TryAcquireAsync("key", default); // exhaust
        var adapter = new RedisSentinelHandshakeRateLimiter(inner);
        var result = await adapter.TryAcquireAsync("key", default);
        result.Should().BeFalse();
    }

    // 14. Null inner → throws ArgumentNullException
    [Fact]
    public void Adapter_NullInner_ThrowsArgumentNullException()
    {
        var act = () => new RedisSentinelHandshakeRateLimiter(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // 15. Adapter implements IHandshakeRateLimiter
    [Fact]
    public void Adapter_ImplementsIHandshakeRateLimiter()
    {
        var inner = new RedisSentinelRateLimiter();
        var adapter = new RedisSentinelHandshakeRateLimiter(inner);
        adapter.Should().BeAssignableTo<IHandshakeRateLimiter>();
    }
#pragma warning restore CS0618
}
