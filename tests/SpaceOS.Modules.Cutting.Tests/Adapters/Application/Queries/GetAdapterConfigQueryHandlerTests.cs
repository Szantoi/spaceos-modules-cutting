using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Application.Adapters.Queries;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application.Queries;

public class GetAdapterConfigQueryHandlerTests
{
    private readonly Mock<ITenantCuttingProviderConfigRepository> _repoMock = new();

    private GetAdapterConfigQueryHandler BuildSut() =>
        new(_repoMock.Object);

    [Fact]
    public async Task Handle_ConfigExists_ReturnsDto()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(
            tenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var result = await sut.Handle(new GetAdapterConfigQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.AdapterName.Should().Be("opticut");
        result.Value.TransportName.Should().Be("file-exchange");
        result.Value.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoConfig_ReturnsNotFound()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();

        _repoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);

        var result = await sut.Handle(new GetAdapterConfigQuery(tenantId), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }
}
