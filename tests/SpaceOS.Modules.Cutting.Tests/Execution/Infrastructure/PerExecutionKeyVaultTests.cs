using FluentAssertions;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Crypto;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class PerExecutionKeyVaultTests
{
    private readonly PerExecutionKeyVault _vault = new();

    [Fact]
    public async Task GenerateKey_ReturnsNonNullKey()
    {
        var key = await _vault.GenerateKeyAsync(Guid.NewGuid(), CancellationToken.None);
        key.Should().NotBeNull();
        key.KeyBytes.Should().HaveCount(32);
    }

    [Fact]
    public async Task GetKey_AfterGenerate_ReturnsKey()
    {
        var executionId = Guid.NewGuid();
        await _vault.GenerateKeyAsync(executionId, CancellationToken.None);

        var key = await _vault.GetKeyAsync(executionId, CancellationToken.None);

        key.Should().NotBeNull();
        key!.KeyBytes.Should().HaveCount(32);
    }

    [Fact]
    public async Task GetKey_WithoutPriorGenerate_ReturnsNull()
    {
        var key = await _vault.GetKeyAsync(Guid.NewGuid(), CancellationToken.None);
        key.Should().BeNull();
    }

    [Fact]
    public async Task EraseKey_AfterErase_ReturnsNull()
    {
        var executionId = Guid.NewGuid();
        await _vault.GenerateKeyAsync(executionId, CancellationToken.None);

        await _vault.EraseAsync(executionId, CancellationToken.None);

        var key = await _vault.GetKeyAsync(executionId, CancellationToken.None);
        key.Should().BeNull();
    }

    [Fact]
    public async Task EraseKey_NotExisting_DoesNotThrow()
    {
        var act = async () => await _vault.EraseAsync(Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GenerateKey_TwoDifferentExecutions_ProduceDifferentKeys()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var key1 = await _vault.GenerateKeyAsync(id1, CancellationToken.None);
        var key2 = await _vault.GenerateKeyAsync(id2, CancellationToken.None);

        key1.KeyBytes.Should().NotBeEquivalentTo(key2.KeyBytes);
    }

    [Fact]
    public async Task GenerateKey_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _vault.GenerateKeyAsync(Guid.NewGuid(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetKey_CancelledToken_ThrowsOperationCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await _vault.GetKeyAsync(Guid.NewGuid(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
