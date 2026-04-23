using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class PanelReservationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid PlanId   = Guid.NewGuid();
    private static readonly Guid SlotId   = Guid.NewGuid();

    private static PanelReservation MakeReservation()
        => PanelReservation.Create(TenantId, PlanId, SlotId, Guid.NewGuid(), "MDF-18mm", 1200m, 800m);

    [Fact]
    public void Create_WithValidArgs_SetsAllProperties()
    {
        var invId = Guid.NewGuid();
        var r = PanelReservation.Create(TenantId, PlanId, SlotId, invId, "MDF-18mm", 1200m, 800m);

        r.TenantId.Should().Be(TenantId);
        r.CuttingPlanId.Should().Be(PlanId);
        r.DaySlotId.Should().Be(SlotId);
        r.InventoryReservationId.Should().Be(invId);
        r.MaterialCode.Should().Be("MDF-18mm");
        r.WidthMm.Should().Be(1200m);
        r.HeightMm.Should().Be(800m);
        r.Status.Should().Be(PanelReservationStatus.Pending);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var r1 = MakeReservation();
        var r2 = MakeReservation();
        r1.Id.Should().NotBe(r2.Id);
    }

    [Fact]
    public void Confirm_FromPending_SetsConfirmed()
    {
        var r = MakeReservation();
        r.Confirm();
        r.Status.Should().Be(PanelReservationStatus.Confirmed);
    }

    [Fact]
    public void Confirm_FromConfirmed_ShouldThrow()
    {
        var r = MakeReservation();
        r.Confirm();
        var act = () => r.Confirm();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Release_FromPending_SetsReleased()
    {
        var r = MakeReservation();
        r.Release();
        r.Status.Should().Be(PanelReservationStatus.Released);
    }

    [Fact]
    public void Release_FromConfirmed_SetsReleased()
    {
        var r = MakeReservation();
        r.Confirm();
        r.Release();
        r.Status.Should().Be(PanelReservationStatus.Released);
    }

    [Fact]
    public void Release_FromReleased_ShouldThrow()
    {
        var r = MakeReservation();
        r.Release();
        var act = () => r.Release();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => PanelReservation.Create(Guid.Empty, PlanId, SlotId, Guid.NewGuid(), "MDF", 1200m, 800m);
        act.Should().Throw<ArgumentException>().WithParameterName("tenantId");
    }

    [Fact]
    public void Create_WithZeroWidthMm_ShouldThrow()
    {
        var act = () => PanelReservation.Create(TenantId, PlanId, SlotId, Guid.NewGuid(), "MDF", 0m, 800m);
        act.Should().Throw<ArgumentException>().WithParameterName("widthMm");
    }

    [Fact]
    public void Create_WithEmptyMaterialCode_ShouldThrow()
    {
        var act = () => PanelReservation.Create(TenantId, PlanId, SlotId, Guid.NewGuid(), "", 1200m, 800m);
        act.Should().Throw<ArgumentException>().WithParameterName("materialCode");
    }
}
