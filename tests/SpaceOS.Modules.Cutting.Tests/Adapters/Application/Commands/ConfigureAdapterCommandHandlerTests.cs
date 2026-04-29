using Ardalis.Result;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Application.Adapters.Commands;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application.Commands;

public class ConfigureAdapterCommandHandlerTests
{
    private readonly Mock<ITenantCuttingProviderConfigRepository> _repoMock = new();
    private readonly Mock<IConfigSecretDetector> _detectorMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly TimeProvider _clock = TimeProvider.System;

    private ConfigureAdapterCommandHandler BuildSut() =>
        new(_repoMock.Object, _detectorMock.Object, _cacheMock.Object, _clock);

    private static ConfigureAdapterCommand MakeCommand(
        Guid? tenantId = null,
        string adapterName = "builtin",
        string transportName = "none",
        string configJson = "{}",
        int expectedVersion = 0) =>
        new(
            tenantId ?? Guid.NewGuid(),
            adapterName,
            transportName,
            configJson,
            1,
            expectedVersion,
            null,
            Guid.NewGuid());

    [Fact]
    public async Task Handle_NewTenant_CreatesConfig()
    {
        var sut = BuildSut();
        var command = MakeCommand();

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Success());
        _repoMock.Setup(r => r.GetByTenantAsync(command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<TenantCuttingProviderConfig>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveAsync(It.Is<string>(k => k.Contains(command.TenantId.ToString())), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingTenantCorrectVersion_Reconfigures()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var command = MakeCommand(tenantId: tenantId, expectedVersion: 1);

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Success());

        var existing = TenantCuttingProviderConfig.Create(
            tenantId, "builtin", "none", "{}", 1, Guid.NewGuid(), _clock).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TenantCuttingProviderConfig>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingTenantVersionMismatch_ReturnsConflict()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        // ExpectedVersion = 99, but actual config version = 1
        var command = MakeCommand(tenantId: tenantId, expectedVersion: 99);

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Success());

        var existing = TenantCuttingProviderConfig.Create(
            tenantId, "builtin", "none", "{}", 1, Guid.NewGuid(), _clock).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Conflict);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TenantCuttingProviderConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SecretInConfigJson_ReturnsInvalid()
    {
        var sut = BuildSut();
        var command = MakeCommand(configJson: "{\"password\": \"abc123xyz\"}");

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Invalid(new ValidationError("Plaintext secret detected.")));

        var result = await sut.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<TenantCuttingProviderConfig>(), It.IsAny<CancellationToken>()), Times.Never);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<TenantCuttingProviderConfig>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidAdapterName_ReturnsInvalid()
    {
        var sut = BuildSut();
        var command = MakeCommand(adapterName: "nonexistent-adapter");

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Success());
        _repoMock.Setup(r => r.GetByTenantAsync(command.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_ValidCommand_InvalidatesCacheForTenant()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var command = MakeCommand(tenantId: tenantId);

        _detectorMock.Setup(d => d.ValidateConfigJson(It.IsAny<string?>()))
            .Returns(Result.Success());
        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        await sut.Handle(command, CancellationToken.None);

        _cacheMock.Verify(
            c => c.RemoveAsync($"adapter-config:{tenantId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
