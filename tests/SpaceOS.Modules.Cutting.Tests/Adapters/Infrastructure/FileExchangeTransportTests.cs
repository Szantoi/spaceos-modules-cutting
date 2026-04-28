using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Infrastructure;

public class FileExchangeTransportTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string AdapterName = "opticut";

    private static AdapterPayload BuildPayload(Guid? tenantId = null, string? adapterName = null) =>
        new AdapterPayload(
            "application/octet-stream",
            new byte[] { 1, 2, 3 },
            new Dictionary<string, string>
            {
                ["tenantId"] = (tenantId ?? TenantId).ToString(),
                ["adapterName"] = adapterName ?? AdapterName
            });

    [Fact]
    public async Task SubmitAsync_ValidPayload_ReturnsSuccess()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        storageMock
            .Setup(s => s.WriteToInboxAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);

        var result = await transport.SubmitAsync(BuildPayload(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CorrelationId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitAsync_MissingTenantIdMetadata_ReturnsInvalid()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);

        var payload = new AdapterPayload(
            "application/octet-stream",
            new byte[] { 1 },
            new Dictionary<string, string> { ["adapterName"] = AdapterName });

        var result = await transport.SubmitAsync(payload, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAsync_MissingAdapterNameMetadata_ReturnsInvalid()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);

        var payload = new AdapterPayload(
            "application/octet-stream",
            new byte[] { 1 },
            new Dictionary<string, string> { ["tenantId"] = TenantId.ToString() });

        var result = await transport.SubmitAsync(payload, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PollResultAsync_NoCompleteFile_ReturnsNotFound()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        storageMock
            .Setup(s => s.TryReadFromOutboxAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);
        var correlationId = $"{TenantId}|{AdapterName}|abc123";

        var result = await transport.PollResultAsync(correlationId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Fact]
    public async Task PollResultAsync_CompleteFileExists_ReturnsPayload()
    {
        var expectedBytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var storageMock = new Mock<ITenantAdapterStorage>();
        storageMock
            .Setup(s => s.TryReadFromOutboxAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBytes);

        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);
        var correlationId = $"{TenantId}|{AdapterName}|abc123";

        var result = await transport.PollResultAsync(correlationId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().BeEquivalentTo(expectedBytes);
    }

    [Fact]
    public async Task PingAsync_ReturnsSuccess()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);

        var result = await transport.PingAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TransportName_IsFileExchange()
    {
        var transport = new FileExchangeTransport(Mock.Of<ITenantAdapterStorage>(), TimeProvider.System);
        transport.TransportName.Should().Be("file-exchange");
    }

    [Fact]
    public async Task SubmitAsync_WritesToStorage()
    {
        var storageMock = new Mock<ITenantAdapterStorage>();
        storageMock
            .Setup(s => s.WriteToInboxAsync(TenantId, AdapterName, It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var transport = new FileExchangeTransport(storageMock.Object, TimeProvider.System);
        await transport.SubmitAsync(BuildPayload(), CancellationToken.None);

        storageMock.Verify(
            s => s.WriteToInboxAsync(TenantId, AdapterName, It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
