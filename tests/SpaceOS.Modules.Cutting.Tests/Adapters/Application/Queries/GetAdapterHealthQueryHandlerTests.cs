using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Application.Adapters.Queries;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Application.Queries;

public class GetAdapterHealthQueryHandlerTests
{
    private readonly Mock<IAdapterHealthRecordRepository> _healthRepoMock = new();
    private readonly Mock<ITenantCuttingProviderConfigRepository> _configRepoMock = new();

    private GetAdapterHealthQueryHandler BuildSut() =>
        new(_healthRepoMock.Object, _configRepoMock.Object);

    [Fact]
    public async Task Handle_HealthRecordExists_ReturnsMappedDto()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(
            tenantId, "opticut", "file-exchange", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        var healthRecord = AdapterHealthRecord.Create(tenantId, "opticut", TimeProvider.System);
        healthRecord.RecordHealthy(TimeProvider.System);

        _configRepoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _healthRepoMock.Setup(r => r.GetAsync(tenantId, "opticut", It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthRecord);

        var result = await sut.Handle(new GetAdapterHealthQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(tenantId);
        result.Value.AdapterName.Should().Be("opticut");
        result.Value.IsHealthy.Should().BeTrue();
        result.Value.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoHealthRecord_ReturnsOptimisticDefault()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();

        _configRepoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantCuttingProviderConfig?)null);
        _healthRepoMock.Setup(r => r.GetAsync(tenantId, "builtin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdapterHealthRecord?)null);

        var result = await sut.Handle(new GetAdapterHealthQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeTrue();   // optimistic default
        result.Value.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UnhealthyRecord_ReturnsFailureDetails()
    {
        var sut = BuildSut();
        var tenantId = Guid.NewGuid();
        var config = TenantCuttingProviderConfig.Create(
            tenantId, "cutrite", "cli-wrapper", "{}", 1, Guid.NewGuid(), TimeProvider.System).Value;

        var healthRecord = AdapterHealthRecord.Create(tenantId, "cutrite", TimeProvider.System);
        healthRecord.RecordFailure("process timeout", TimeProvider.System);
        healthRecord.RecordFailure("process timeout", TimeProvider.System);

        _configRepoMock.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);
        _healthRepoMock.Setup(r => r.GetAsync(tenantId, "cutrite", It.IsAny<CancellationToken>()))
            .ReturnsAsync(healthRecord);

        var result = await sut.Handle(new GetAdapterHealthQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeFalse();
        result.Value.ConsecutiveFailures.Should().Be(2);
    }
}
