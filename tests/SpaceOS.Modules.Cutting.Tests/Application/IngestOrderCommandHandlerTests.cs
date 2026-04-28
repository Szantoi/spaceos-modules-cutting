using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Cutting.Application.Commands.IngestOrder;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;
using SpaceOS.Modules.Cutting.Domain.Services;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Application;

public class IngestOrderCommandHandlerTests
{
    private readonly Mock<ICuttingRepository> _repoMock = new();
    private readonly ICapacityModel _capacityModel = new AreaCapacityModel();

    private IngestOrderCommandHandler CreateHandler()
        => new(_repoMock.Object, _capacityModel);

    private static DaySlot CreateOpenSlot(decimal capacityHours = 8m, int daysFromNow = 1)
    {
        var planId = Guid.NewGuid();
        return DaySlot.Create(planId, DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(daysFromNow)), capacityHours);
    }

    // ── 1. Happy path: 3 items → 3 CuttingJobs created ───────────────────────

    [Fact]
    public async Task Handle_HappyPath_3Items_Creates3Jobs()
    {
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var slot = CreateOpenSlot();

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot> { slot });

        var items = new List<IngestOrderItem>
        {
            new("Door Panel A", 600m, 2000m, "MDF 18mm", GrainDirection.Vertical, 1),
            new("Door Panel B", 400m, 800m, "MDF 18mm", GrainDirection.Horizontal, 1),
            new("Side Panel", 300m, 600m, "HDF 3mm", GrainDirection.None, 1),
        };

        var handler = CreateHandler();
        var result = await handler.Handle(new IngestOrderCommand(orderId, tenantId, items), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);
        slot.Jobs.Should().HaveCount(3);
    }

    // ── 2. CuttingJob dimensions > 0 ─────────────────────────────────────────

    [Fact]
    public async Task Handle_JobDimensions_AreFromItems()
    {
        var orderId = Guid.NewGuid();
        var slot = CreateOpenSlot();

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot> { slot });

        var items = new List<IngestOrderItem>
        {
            new("Panel X", 750m, 1200m, "MDF 18mm", GrainDirection.Vertical, 1),
        };

        var handler = CreateHandler();
        await handler.Handle(new IngestOrderCommand(orderId, Guid.NewGuid(), items), default);

        var job = slot.Jobs.Single();
        job.WidthMm.Should().Be(750m);
        job.HeightMm.Should().Be(1200m);
    }

    // ── 3. Material + GrainDirection persisted ─────────────────────────────────

    [Fact]
    public async Task Handle_MaterialAndGrainDirection_Persisted()
    {
        var orderId = Guid.NewGuid();
        var slot = CreateOpenSlot();

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot> { slot });

        var items = new List<IngestOrderItem>
        {
            new("Panel Z", 500m, 500m, "Plywood 12mm", GrainDirection.Horizontal, 1),
        };

        var handler = CreateHandler();
        await handler.Handle(new IngestOrderCommand(orderId, Guid.NewGuid(), items), default);

        var job = slot.Jobs.Single();
        job.Material.Should().Be("Plywood 12mm");
        job.GrainDirection.Should().Be(GrainDirection.Horizontal);
    }

    // ── 4. DaySlot assignment: job goes to earliest slot ──────────────────────

    [Fact]
    public async Task Handle_AssignsToEarliestOpenSlot()
    {
        var orderId = Guid.NewGuid();
        var slot1 = CreateOpenSlot(daysFromNow: 5);
        var slot2 = CreateOpenSlot(daysFromNow: 1); // earlier

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        // Repo returns slots ordered by date — slot2 is first
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot> { slot2, slot1 });

        var items = new List<IngestOrderItem>
        {
            new("Panel", 300m, 400m, "MDF 18mm", GrainDirection.None, 1),
        };

        var handler = CreateHandler();
        await handler.Handle(new IngestOrderCommand(orderId, Guid.NewGuid(), items), default);

        slot2.Jobs.Should().HaveCount(1, "job should go to the earlier slot");
        slot1.Jobs.Should().BeEmpty();
    }

    // ── 5. Duplicate orderId → idempotent (0 new jobs) ───────────────────────

    [Fact]
    public async Task Handle_DuplicateOrderId_ReturnsZero()
    {
        var orderId = Guid.NewGuid();

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(true);

        var items = new List<IngestOrderItem>
        {
            new("Panel", 500m, 500m, "MDF 18mm", GrainDirection.None, 1),
        };

        var handler = CreateHandler();
        var result = await handler.Handle(new IngestOrderCommand(orderId, Guid.NewGuid(), items), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0, "idempotent: already ingested");
    }

    // ── 6. Empty items → Invalid ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyItems_ReturnsInvalid()
    {
        var handler = CreateHandler();
        var result = await handler.Handle(
            new IngestOrderCommand(Guid.NewGuid(), Guid.NewGuid(), new List<IngestOrderItem>()), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Invalid);
    }

    // ── 7. No open DaySlots → Error ────────────────────────────────────────

    [Fact]
    public async Task Handle_NoOpenSlots_ReturnsError()
    {
        var orderId = Guid.NewGuid();
        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot>());

        var items = new List<IngestOrderItem>
        {
            new("Panel", 500m, 500m, "MDF 18mm", GrainDirection.None, 1),
        };

        var handler = CreateHandler();
        var result = await handler.Handle(new IngestOrderCommand(orderId, Guid.NewGuid(), items), default);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.Error);
    }

    // ── 8. Tenant isolation: OrderId scoped correctly ─────────────────────────

    [Fact]
    public async Task Handle_TenantIsolation_JobsCreatedWithCorrectOrderId()
    {
        var orderId = Guid.NewGuid();
        var tenantA = Guid.NewGuid();
        var slot = CreateOpenSlot();

        _repoMock.Setup(r => r.HasJobsForOrderAsync(orderId, default)).ReturnsAsync(false);
        _repoMock.Setup(r => r.GetOpenSlotsOrderedByDateAsync(default))
            .ReturnsAsync(new List<DaySlot> { slot });

        var items = new List<IngestOrderItem>
        {
            new("Panel", 400m, 400m, "MDF 18mm", GrainDirection.None, 1),
        };

        var handler = CreateHandler();
        await handler.Handle(new IngestOrderCommand(orderId, tenantA, items), default);

        // All jobs should belong to the same order
        slot.Jobs.Should().AllSatisfy(j => j.OrderId.Should().Be(orderId));
        _repoMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
