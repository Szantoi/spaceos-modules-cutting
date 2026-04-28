using FluentAssertions;
using SpaceOS.Modules.Cutting.Infrastructure.Outbox;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Execution.Infrastructure;

public class LocalOutboxMessageTests
{
    [Fact]
    public void Create_WithValidArgs_SetsProperties()
    {
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var aggId = Guid.NewGuid();

        var msg = LocalOutboxMessage.Create(tenantId, "SomeEvent", "{}", batchId, 0, aggId, "MyAggregate");

        msg.TenantId.Should().Be(tenantId);
        msg.EventType.Should().Be("SomeEvent");
        msg.PayloadJson.Should().Be("{}");
        msg.BatchId.Should().Be(batchId);
        msg.BatchSequenceNumber.Should().Be(0);
        msg.AggregateId.Should().Be(aggId);
        msg.AggregateType.Should().Be("MyAggregate");
        msg.Status.Should().Be(LocalOutboxStatus.Pending);
        msg.Attempts.Should().Be(0);
        msg.ProcessedAt.Should().BeNull();
        msg.LastError.Should().BeNull();
        msg.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_WithoutBatchInfo_LeavesOptionalPropertiesNull()
    {
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");

        msg.BatchId.Should().BeNull();
        msg.BatchSequenceNumber.Should().BeNull();
        msg.AggregateId.Should().BeNull();
        msg.AggregateType.Should().BeNull();
    }

    [Fact]
    public void Create_NullEventType_ThrowsArgumentNullException()
    {
        var act = () => LocalOutboxMessage.Create(Guid.NewGuid(), null!, "{}");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullPayload_ThrowsArgumentNullException()
    {
        var act = () => LocalOutboxMessage.Create(Guid.NewGuid(), "Event", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkProcessed_SetsStatusAndProcessedAt()
    {
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");
        var before = DateTimeOffset.UtcNow;

        msg.MarkProcessed();

        msg.Status.Should().Be(LocalOutboxStatus.Processed);
        msg.ProcessedAt.Should().NotBeNull();
        msg.ProcessedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void MarkFailed_IncrementsAttemptsAndSetsLastError()
    {
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");

        msg.MarkFailed("timeout error");

        msg.Status.Should().Be(LocalOutboxStatus.Failed);
        msg.Attempts.Should().Be(1);
        msg.LastError.Should().Be("timeout error");
    }

    [Fact]
    public void MarkFailed_CalledTwice_IncrementsAttemptsEachTime()
    {
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");

        msg.MarkFailed("first error");
        msg.MarkFailed("second error");

        msg.Attempts.Should().Be(2);
        msg.LastError.Should().Be("second error");
    }

    [Fact]
    public void MarkFailed_NullError_ThrowsArgumentNullException()
    {
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");
        var act = () => msg.MarkFailed(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void OccurredAt_IsSetOnCreate()
    {
        var before = DateTimeOffset.UtcNow;
        var msg = LocalOutboxMessage.Create(Guid.NewGuid(), "Event", "{}");
        msg.OccurredAt.Should().BeOnOrAfter(before);
    }
}
