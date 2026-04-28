using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Audit;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.HashChain;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Inventory;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.StageRegistry;
using SpaceOS.Modules.Cutting.Execution.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class SerilogCuttingAuditLoggerTests
{
    [Fact]
    public async Task LogSecurityEventAsync_DoesNotThrow()
    {
        var logger = new SerilogCuttingAuditLogger(NullLogger<SerilogCuttingAuditLogger>.Instance);
        var act = async () => await logger.LogSecurityEventAsync(
            "BADGE_SCAN", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LogSecurityEventAsync_CancelledToken_ThrowsOperationCancelled()
    {
        var logger = new SerilogCuttingAuditLogger(NullLogger<SerilogCuttingAuditLogger>.Instance);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await logger.LogSecurityEventAsync(
            "EVENT", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

public class NullCuttingHashChainSinkTests
{
    [Fact]
    public async Task AppendAsync_DoesNotThrow()
    {
        var sink = new NullCuttingHashChainSink(NullLogger<NullCuttingHashChainSink>.Instance);
        var act = async () => await sink.AppendAsync(
            Guid.NewGuid(), Guid.NewGuid(), "abc123", CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}

public class NullStageRegistryTests
{
    [Fact]
    public async Task NotifyMilestoneAsync_DoesNotThrow()
    {
        var registry = new NullStageRegistry(NullLogger<NullStageRegistry>.Instance);
        var act = async () => await registry.NotifyMilestoneAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), MilestoneKind.PanelCompletion, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}

public class NullOffcutNotificationSinkTests
{
    [Fact]
    public async Task NotifyAsync_DoesNotThrow()
    {
        var sink = new NullOffcutNotificationSink(NullLogger<NullOffcutNotificationSink>.Instance);
        var act = async () => await sink.NotifyAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 500m, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
