using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Events;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class CuttingSheetTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IEnumerable<CuttingLine> OneValidLine() =>
        new[] { CuttingLine.Create(Guid.NewGuid(), "Panel A", "MDF 18mm", 600, 400, 18, 2) };

    [Fact]
    public void Create_WithValidData_ShouldBeDraft()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Status.Should().Be(CuttingSheetStatus.Draft);
        sheet.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Submit_FromDraft_ShouldTransitionToSubmitted()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Submit();
        sheet.Status.Should().Be(CuttingSheetStatus.Submitted);
    }

    [Fact]
    public void Submit_ShouldRaiseCuttingSheetSubmittedEvent()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Submit();
        sheet.DomainEvents.Should().ContainSingle(e => e is CuttingSheetSubmittedEvent);
    }

    [Fact]
    public void Submit_AlreadySubmitted_ShouldThrow()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Submit();
        var act = () => sheet.Submit();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_FromSubmitted_ShouldTransitionToCompleted()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Submit();
        sheet.PopDomainEvents();
        sheet.Complete();
        sheet.Status.Should().Be(CuttingSheetStatus.Completed);
    }

    [Fact]
    public void Complete_FromDraft_ShouldThrow()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        var act = () => sheet.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WithEmptyTenantId_ShouldThrow()
    {
        var act = () => CuttingSheet.Create(Guid.Empty, "ORD-001", OneValidLine());
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PopDomainEvents_ShouldClearEvents()
    {
        var sheet = CuttingSheet.Create(TenantId, "ORD-001", OneValidLine());
        sheet.Submit();
        sheet.DomainEvents.Should().NotBeEmpty();
        sheet.PopDomainEvents();
        sheet.DomainEvents.Should().BeEmpty();
    }
}
