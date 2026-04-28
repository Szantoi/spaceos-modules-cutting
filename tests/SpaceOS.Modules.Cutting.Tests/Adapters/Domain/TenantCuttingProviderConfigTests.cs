using Ardalis.Result;
using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Domain.Adapters.Events;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Domain;

public class TenantCuttingProviderConfigTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly TimeProvider Clock = TimeProvider.System;

    private static Result<TenantCuttingProviderConfig> CreateDefault(
        string adapter = "opticut",
        string transport = "file-exchange") =>
        TenantCuttingProviderConfig.Create(
            TenantId, adapter, transport, "{}", 1, ActorId, Clock);

    [Fact]
    public void Create_ValidArgs_ReturnsSuccess()
    {
        var result = CreateDefault();
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().Be(TenantId);
        result.Value.AdapterName.Should().Be("opticut");
        result.Value.TransportName.Should().Be("file-exchange");
        result.Value.IsEnabled.Should().BeTrue();
        result.Value.Version.Should().Be(1);
    }

    [Fact]
    public void Create_EmptyTenantId_ReturnsInvalid()
    {
        var result = TenantCuttingProviderConfig.Create(
            Guid.Empty, "builtin", "none", "{}", 1, ActorId, Clock);
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public void Create_EmptyActorId_ReturnsInvalid()
    {
        var result = TenantCuttingProviderConfig.Create(
            TenantId, "builtin", "none", "{}", 1, Guid.Empty, Clock);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Create_InvalidAdapterName_ReturnsInvalid()
    {
        var result = TenantCuttingProviderConfig.Create(
            TenantId, "unknown-adapter", "none", "{}", 1, ActorId, Clock);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("AdapterName"));
    }

    [Fact]
    public void Create_InvalidTransportName_ReturnsInvalid()
    {
        var result = TenantCuttingProviderConfig.Create(
            TenantId, "builtin", "smtp", "{}", 1, ActorId, Clock);
        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage.Contains("TransportName"));
    }

    [Theory]
    [InlineData("builtin", "none")]
    [InlineData("opticut", "file-exchange")]
    [InlineData("cutrite", "rest-api")]
    [InlineData("manual", "cli-wrapper")]
    public void Create_AllAllowedAdapterTransportCombinations_Succeed(string adapter, string transport)
    {
        CreateDefault(adapter, transport).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_RaisesTenantAdapterConfiguredEvent()
    {
        var config = CreateDefault().Value;
        var events = config.PopDomainEvents();
        events.Should().ContainSingle(e => e is TenantAdapterConfigured);
        var evt = (TenantAdapterConfigured)events[0];
        evt.TenantId.Should().Be(TenantId);
        evt.AdapterName.Should().Be("opticut");
        evt.ActorId.Should().Be(ActorId);
    }

    [Fact]
    public void Reconfigure_ValidArgs_UpdatesAndRaisesEvent()
    {
        var config = CreateDefault().Value;
        config.PopDomainEvents(); // clear creation event

        var result = config.Reconfigure(
            "cutrite", "rest-api", "{\"endpoint\":\"${secret:url}\"}",
            2, 1, ActorId, "Switching to CutRite", Clock);

        result.IsSuccess.Should().BeTrue();
        config.AdapterName.Should().Be("cutrite");
        config.Version.Should().Be(2);

        var events = config.PopDomainEvents();
        events.Should().ContainSingle(e => e is TenantAdapterReconfigured);
        var evt = (TenantAdapterReconfigured)events[0];
        evt.ChangeReason.Should().Be("Switching to CutRite");
    }

    [Fact]
    public void Reconfigure_VersionMismatch_ReturnsConflict()
    {
        var config = CreateDefault().Value;
        var result = config.Reconfigure("builtin", "none", "{}", 1, 99, ActorId, null, Clock);
        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public void Reconfigure_InvalidAdapter_ReturnsInvalid()
    {
        var config = CreateDefault().Value;
        var result = config.Reconfigure("bad", "none", "{}", 1, 1, ActorId, null, Clock);
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Disable_EnabledConfig_SetsIsEnabledFalse()
    {
        var config = CreateDefault().Value;
        config.PopDomainEvents();

        var result = config.Disable(1, ActorId, Clock);

        result.IsSuccess.Should().BeTrue();
        config.IsEnabled.Should().BeFalse();
        config.Version.Should().Be(2);
    }

    [Fact]
    public void Disable_AlreadyDisabled_IsIdempotent()
    {
        var config = CreateDefault().Value;
        config.Disable(1, ActorId, Clock);
        config.PopDomainEvents();

        var result = config.Disable(2, ActorId, Clock);

        result.IsSuccess.Should().BeTrue();
        config.Version.Should().Be(2); // no increment on idempotent disable
    }

    [Fact]
    public void Disable_VersionMismatch_ReturnsConflict()
    {
        var config = CreateDefault().Value;
        var result = config.Disable(99, ActorId, Clock);
        result.Status.Should().Be(ResultStatus.Conflict);
    }

    [Fact]
    public void Disable_RaisesTenantAdapterDisabledEvent()
    {
        var config = CreateDefault().Value;
        config.PopDomainEvents();

        config.Disable(1, ActorId, Clock);

        var events = config.PopDomainEvents();
        events.Should().ContainSingle(e => e is TenantAdapterDisabled);
    }

    [Fact]
    public void Disable_AlreadyDisabled_DoesNotRaiseEvent()
    {
        var config = CreateDefault().Value;
        config.Disable(1, ActorId, Clock);
        config.PopDomainEvents();

        config.Disable(2, ActorId, Clock);

        var events = config.PopDomainEvents();
        events.Should().BeEmpty();
    }
}
