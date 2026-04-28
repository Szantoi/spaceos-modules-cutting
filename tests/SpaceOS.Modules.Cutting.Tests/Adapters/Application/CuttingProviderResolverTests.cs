using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application;

public class CuttingProviderResolverTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static ICuttingProvider MockProvider() => Mock.Of<ICuttingProvider>();

    private static IAdapterFactory BuildFactory(params string[] adapterNames)
    {
        var registrations = adapterNames
            .Select(n => new KeyedAdapterRegistration(n, MockProvider()))
            .ToArray();
        return new AdapterFactory(registrations);
    }

    /// <summary>
    /// In-process cache using a simple dictionary — avoids any NuGet versioning conflict
    /// with Microsoft.Extensions.Caching.Memory while providing correct IDistributedCache behaviour.
    /// </summary>
    private static IDistributedCache BuildMemoryCache()
    {
        var mock = new Mock<IDistributedCache>();
        var store = new Dictionary<string, byte[]?>();

        mock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                store.TryGetValue(key, out var v) ? v : null);

        mock.Setup(c => c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback((string key, byte[] value, DistributedCacheEntryOptions _, CancellationToken _) =>
                store[key] = value)
            .Returns(Task.CompletedTask);

        mock.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string key, CancellationToken _) => store.Remove(key))
            .Returns(Task.CompletedTask);

        return mock.Object;
    }

    private static CuttingProviderResolver BuildResolver(
        IAdapterFactory factory,
        ITenantCuttingProviderConfigRepository configRepo,
        IDistributedCache? cache = null,
        IAdapterCallAuditWriter? auditWriter = null)
    {
        return new CuttingProviderResolver(
            factory,
            configRepo,
            cache ?? BuildMemoryCache(),
            auditWriter ?? Mock.Of<IAdapterCallAuditWriter>(),
            TimeProvider.System,
            NullLogger<CuttingProviderResolver>.Instance,
            () => TenantId);
    }

    [Fact]
    public async Task ResolveAsync_NoConfig_ReturnsBuiltinAdapter()
    {
        var factory = BuildFactory("builtin", "opticut");
        var configRepo = Mock.Of<ITenantCuttingProviderConfigRepository>(
            r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()) == Task.FromResult<TenantCuttingProviderConfig?>(null));

        var resolver = BuildResolver(factory, configRepo);
        var provider = await resolver.ResolveAsync(CancellationToken.None);

        provider.Should().NotBeNull();
        // The builtin adapter was returned
        Mock.Get(configRepo).Verify(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_EnabledConfig_ReturnsNamedAdapter()
    {
        var opticut = MockProvider();
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", MockProvider()),
            new KeyedAdapterRegistration("opticut", opticut)
        });

        var config = TenantCuttingProviderConfig.Create(
            TenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        var configRepo = Mock.Of<ITenantCuttingProviderConfigRepository>(
            r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()) == Task.FromResult<TenantCuttingProviderConfig?>(config));

        var resolver = BuildResolver(factory, configRepo);
        var provider = await resolver.ResolveAsync(CancellationToken.None);

        provider.Should().BeSameAs(opticut);
    }

    [Fact]
    public async Task ResolveAsync_DisabledConfig_ReturnsBuiltinAdapter()
    {
        var builtin = MockProvider();
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", builtin),
            new KeyedAdapterRegistration("opticut", MockProvider())
        });

        var config = TenantCuttingProviderConfig.Create(
            TenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;
        config.Disable(1, Guid.NewGuid(), TimeProvider.System);

        var configRepo = Mock.Of<ITenantCuttingProviderConfigRepository>(
            r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()) == Task.FromResult<TenantCuttingProviderConfig?>(config));

        var resolver = BuildResolver(factory, configRepo);
        var provider = await resolver.ResolveAsync(CancellationToken.None);

        provider.Should().BeSameAs(builtin);
    }

    [Fact]
    public async Task ResolveAsync_SecondCall_UsesCachedValue()
    {
        var factory = BuildFactory("builtin");
        var configRepo = new Mock<ITenantCuttingProviderConfigRepository>();
        configRepo
            .Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        var resolver = BuildResolver(factory, configRepo.Object);

        await resolver.ResolveAsync(CancellationToken.None);
        await resolver.ResolveAsync(CancellationToken.None);

        // Repository should only be called once — second call hits cache
        configRepo.Verify(
            r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_AdapterNotRegistered_FallsBackToBuiltin()
    {
        var builtin = MockProvider();
        var factory = new AdapterFactory(new[]
        {
            new KeyedAdapterRegistration("builtin", builtin)
            // "opticut" is NOT registered
        });

        var config = TenantCuttingProviderConfig.Create(
            TenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        var configRepo = Mock.Of<ITenantCuttingProviderConfigRepository>(
            r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()) == Task.FromResult<TenantCuttingProviderConfig?>(config));

        var auditWriter = new Mock<IAdapterCallAuditWriter>();
        var resolver = BuildResolver(factory, configRepo, auditWriter: auditWriter.Object);

        var provider = await resolver.ResolveAsync(CancellationToken.None);

        provider.Should().BeSameAs(builtin);
        auditWriter.Verify(
            w => w.RecordCapabilityFallbackAsync("opticut", "registration-missing", TenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
