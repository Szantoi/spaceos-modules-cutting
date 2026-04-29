using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Application.Adapters.Commands;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application.Commands;

public class TestAdapterCommandHandlerTests
{
    private readonly Mock<ITenantCuttingProviderConfigRepository> _repoMock = new();
    private readonly Mock<IAdapterFactory> _factoryMock = new();

    private TestAdapterCommandHandler BuildSut() =>
        new(_repoMock.Object, _factoryMock.Object);

    [Fact]
    public async Task Handle_NoConfig_ReturnsHealthyBuiltin()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        _factoryMock.Setup(f => f.RegisteredAdapterNames)
            .Returns(new[] { "builtin", "manual" });

        var result = await sut.Handle(new TestAdapterCommand(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeTrue();
        result.Value.Message.Should().Contain("builtin");
    }

    [Fact]
    public async Task Handle_EnabledBuiltinConfig_ReturnsHealthy()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(tenantId, "builtin", "none", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _factoryMock.Setup(f => f.RegisteredAdapterNames)
            .Returns(new[] { "builtin" });

        var result = await sut.Handle(new TestAdapterCommand(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EnabledManualConfig_ReturnsHealthy()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(tenantId, "manual", "none", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _factoryMock.Setup(f => f.RegisteredAdapterNames)
            .Returns(new[] { "manual" });

        var result = await sut.Handle(new TestAdapterCommand(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AdapterNotRegistered_ReturnsUnhealthy()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(tenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _factoryMock.Setup(f => f.RegisteredAdapterNames)
            .Returns(new[] { "builtin" }); // opticut not registered

        var result = await sut.Handle(new TestAdapterCommand(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeFalse();
        result.Value.Message.Should().Contain("not registered");
    }

    [Fact]
    public async Task Handle_ExternalAdapterThrows_ReturnsUnhealthy()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(tenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        var providerMock = new Mock<ICuttingProvider>();
        providerMock.Setup(p => p.GetNestingResultAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("connection refused"));

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _factoryMock.Setup(f => f.RegisteredAdapterNames)
            .Returns(new[] { "opticut" });
        _factoryMock.Setup(f => f.GetByName("opticut"))
            .Returns(providerMock.Object);

        var result = await sut.Handle(new TestAdapterCommand(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeFalse();
        result.Value.Message.Should().Contain("unreachable");
    }
}
