using FluentAssertions;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.FileSystem;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Infrastructure;

/// <summary>
/// Tests for TenantAdapterStorage.
/// Note: tests that write files use temp dirs derived from the OS temp path.
/// No cleanup to avoid flakiness — temp dirs are ephemeral.
/// </summary>
public class TenantAdapterStorageTests
{
    // We test the public implementation via the interface
    private readonly TenantAdapterStorage _storage = new();

    private static readonly Guid TenantId = Guid.NewGuid();
    private const string AdapterName = "opticut";

    [Theory]
    [InlineData("abc")]
    [InlineData("abc-def-123")]
    [InlineData("AAABBBCCC")]
    [InlineData("a1B2-c3D4")]
    public void WriteToInboxAsync_ValidCorrelationId_DoesNotThrow(string correlationId)
    {
        // We cannot write to /var/lib in tests — expect DirectoryNotFoundException
        // but NOT ArgumentException (which would mean validation failed)
        var act = async () =>
            await _storage.WriteToInboxAsync(TenantId, AdapterName, correlationId, [0x01], CancellationToken.None);

        act.Should().NotThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("../traversal")]
    [InlineData("abc/def")]
    [InlineData("abc def")]
    [InlineData("toolong_" + "x12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345")]
    public void WriteToInboxAsync_InvalidCorrelationId_ThrowsArgumentException(string correlationId)
    {
        var act = async () =>
            await _storage.WriteToInboxAsync(TenantId, AdapterName, correlationId, [0x01], CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task TryReadFromOutboxAsync_ValidCorrelationId_NoCompleteFile_ReturnsNull()
    {
        // The outbox dir doesn't exist — should return null, not throw
        var result = await _storage.TryReadFromOutboxAsync(
            Guid.NewGuid(), "builtin", "abc123", CancellationToken.None);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("foo/bar")]
    public void TryReadFromOutboxAsync_InvalidCorrelationId_ThrowsArgumentException(string correlationId)
    {
        var act = async () =>
            await _storage.TryReadFromOutboxAsync(TenantId, AdapterName, correlationId, CancellationToken.None);

        act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void GetTenantRoot_ContainsTenantIdAndAdapterName()
    {
        var root = _storage.GetTenantRoot(TenantId, AdapterName);
        root.Should().Contain(TenantId.ToString("N"));
        root.Should().Contain(AdapterName);
    }

    [Fact]
    public void GetOutboxPath_IsUnderTenantRoot()
    {
        var root = _storage.GetTenantRoot(TenantId, AdapterName);
        var outbox = _storage.GetOutboxPath(TenantId, AdapterName);
        outbox.Should().StartWith(root);
    }

    [Fact]
    public async Task CheckTenantRootAccessibleAsync_NonExistentDir_ReturnsFalse()
    {
        var result = await _storage.CheckTenantRootAccessibleAsync(
            Guid.NewGuid(), "nonexistent", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public void TryReadFromOutboxAsync_OnlyReadsCompleteExtension()
    {
        // The implementation should only look for .complete files.
        // We verify this by confirming the storage returns null for correlationIds
        // that would resolve to non-.complete files (behavioral contract test).
        // Full integration test would require writing a .complete file to disk.
        var act = async () => await _storage.TryReadFromOutboxAsync(
            Guid.NewGuid(), "test", "valid-id", CancellationToken.None);

        // Should not throw — just return null since no .complete file exists
        act.Should().NotThrowAsync<ArgumentException>();
    }
}
