using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Adapters;
using SpaceOS.Modules.Cutting.Domain.Adapters.Events;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Adapters.Domain;

public class AdapterHealthRecordTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string AdapterName = "opticut";
    private static readonly TimeProvider Clock = TimeProvider.System;

    [Fact]
    public void Create_ValidArgs_StartsHealthy()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.TenantId.Should().Be(TenantId);
        record.AdapterName.Should().Be(AdapterName);
        record.IsHealthy.Should().BeTrue();
        record.ConsecutiveFailures.Should().Be(0);
        record.LastSuccessAt.Should().BeNull();
        record.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyTenantId_Throws()
    {
        var act = () => AdapterHealthRecord.Create(Guid.Empty, AdapterName, Clock);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_EmptyAdapterName_Throws()
    {
        var act = () => AdapterHealthRecord.Create(TenantId, string.Empty, Clock);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordHealthy_SetsIsHealthyAndLastSuccessAt()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);

        record.RecordHealthy(Clock);

        record.IsHealthy.Should().BeTrue();
        record.LastSuccessAt.Should().NotBeNull();
        record.ConsecutiveFailures.Should().Be(0);
        record.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RecordHealthy_AfterFailure_ResetsConsecutiveFailures()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.RecordFailure("error 1", Clock);
        record.RecordFailure("error 2", Clock);

        record.RecordHealthy(Clock);

        record.ConsecutiveFailures.Should().Be(0);
        record.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public void RecordHealthy_AfterFailure_RaisesAdapterHealthRecoveredEvent()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.RecordFailure("error", Clock);
        record.PopDomainEvents();

        record.RecordHealthy(Clock);

        var events = record.PopDomainEvents();
        events.Should().ContainSingle(e => e is AdapterHealthRecovered);
        var evt = (AdapterHealthRecovered)events[0];
        evt.TenantId.Should().Be(TenantId);
        evt.AdapterName.Should().Be(AdapterName);
    }

    [Fact]
    public void RecordHealthy_WhenAlreadyHealthy_DoesNotRaiseRecoveredEvent()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.PopDomainEvents();

        record.RecordHealthy(Clock);

        var events = record.PopDomainEvents();
        events.Should().BeEmpty();
    }

    [Fact]
    public void RecordFailure_SetsIsHealthyFalse()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);

        record.RecordFailure("Connection refused", Clock);

        record.IsHealthy.Should().BeFalse();
        record.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void RecordFailure_IncrementsConsecutiveFailures()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.RecordFailure("e1", Clock);
        record.RecordFailure("e2", Clock);
        record.RecordFailure("e3", Clock);

        record.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void RecordFailure_SanitizesControlCharsInErrorMessage()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);

        record.RecordFailure("Error\r\nInjected\x00", Clock);

        record.LastErrorMessage.Should().NotContain("\r");
        record.LastErrorMessage.Should().NotContain("\n");
        record.LastErrorMessage.Should().NotContain("\x00");
    }

    [Fact]
    public void RecordFailure_RaisesAdapterHealthFailedEvent()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.PopDomainEvents();

        record.RecordFailure("timeout", Clock);

        var events = record.PopDomainEvents();
        events.Should().ContainSingle(e => e is AdapterHealthFailed);
        var evt = (AdapterHealthFailed)events[0];
        evt.TenantId.Should().Be(TenantId);
        evt.AdapterName.Should().Be(AdapterName);
        evt.ConsecutiveFailures.Should().Be(1);
    }

    [Fact]
    public void RecordFailure_ErrorMessageStoredSanitized()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        record.RecordFailure("Error\x01\x02", Clock);
        record.LastErrorMessage.Should().Be("Error");
    }

    [Fact]
    public void RecordFailure_TruncatesLongErrorMessage()
    {
        var record = AdapterHealthRecord.Create(TenantId, AdapterName, Clock);
        var longError = new string('E', 9000);

        record.RecordFailure(longError, Clock);

        record.LastErrorMessage!.Length.Should().Be(8000);
    }
}
