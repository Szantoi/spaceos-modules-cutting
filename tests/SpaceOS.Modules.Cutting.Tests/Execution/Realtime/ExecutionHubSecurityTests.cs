using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SpaceOS.Modules.Cutting.Execution.Infrastructure.Realtime;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Realtime;

public sealed class ExecutionHubSecurityTests
{
    [Fact]
    public async Task JoinExecution_ConflictingClaims_UsesCanonicalTenantClaim()
    {
        var canonicalTenantId = Guid.NewGuid();
        var legacyTenantId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var (hub, _, groups) = CreateHub(
            new Claim("tid", canonicalTenantId.ToString()),
            new Claim("tenant_id", legacyTenantId.ToString()));

        await hub.JoinExecution(executionId);

        groups.Verify(
            groupManager => groupManager.AddToGroupAsync(
                "connection-1",
                $"{canonicalTenantId:D}:{executionId:D}",
                It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            groupManager => groupManager.AddToGroupAsync(
                "connection-1",
                It.Is<string>(group => group.StartsWith(legacyTenantId.ToString(), StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinExecution_MalformedCanonicalClaim_DoesNotFallBackToLegacyClaim()
    {
        var legacyTenantId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var (hub, context, groups) = CreateHub(
            new Claim("tid", "not-a-guid"),
            new Claim("tenant_id", legacyTenantId.ToString()));

        await hub.JoinExecution(executionId);

        context.Verify(callerContext => callerContext.Abort(), Times.Once);
        groups.Verify(
            groupManager => groupManager.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinExecution_CanonicalClaimAbsent_UsesValidLegacyClaim()
    {
        var legacyTenantId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var (hub, context, groups) = CreateHub(new Claim("tenant_id", legacyTenantId.ToString()));

        await hub.JoinExecution(executionId);

        context.Verify(callerContext => callerContext.Abort(), Times.Never);
        groups.Verify(
            groupManager => groupManager.AddToGroupAsync(
                "connection-1",
                $"{legacyTenantId:D}:{executionId:D}",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static (ExecutionHub Hub, Mock<HubCallerContext> Context, Mock<IGroupManager> Groups)
        CreateHub(params Claim[] claims)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var context = new Mock<HubCallerContext>();
        context.SetupGet(callerContext => callerContext.User).Returns(principal);
        context.SetupGet(callerContext => callerContext.ConnectionId).Returns("connection-1");

        var groups = new Mock<IGroupManager>();
        groups.Setup(groupManager => groupManager.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var hub = new ExecutionHub
        {
            Context = context.Object,
            Groups = groups.Object
        };

        hub.Should().NotBeNull();
        return (hub, context, groups);
    }
}
