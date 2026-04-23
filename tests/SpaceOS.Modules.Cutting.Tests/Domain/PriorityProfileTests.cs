using FluentAssertions;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Domain;

public class PriorityProfileTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidArgs_SetsAllProperties()
    {
        var profile = PriorityProfile.Create(TenantId, "Test", "area-v1", "warn-and-apply-v1", "maxcut-v1");

        profile.TenantId.Should().Be(TenantId);
        profile.Name.Should().Be("Test");
        profile.CapacityModelId.Should().Be("area-v1");
        profile.ReworkPolicyId.Should().Be("warn-and-apply-v1");
        profile.PlanningStrategyId.Should().Be("maxcut-v1");
        profile.IsDefault.Should().BeFalse();
        profile.Rules.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithNullTenantId_CreatesGlobalPreset()
    {
        var profile = PriorityProfile.Create(null, "Global", "area-v1", "warn-and-apply-v1", "fifo");

        profile.TenantId.Should().BeNull("global presets have no tenant");
    }

    [Fact]
    public void Create_WithIsDefaultTrue_SetsDefaultFlag()
    {
        var profile = PriorityProfile.Create(TenantId, "Default", "area-v1", "warn-and-apply-v1", "fifo", isDefault: true);

        profile.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void SetDefault_ChangesIsDefaultValue()
    {
        var profile = PriorityProfile.Create(TenantId, "Test", "area-v1", "warn-and-apply-v1", "maxcut-v1");

        profile.SetDefault(true);
        profile.IsDefault.Should().BeTrue();

        profile.SetDefault(false);
        profile.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void AddRule_IncreasesRulesCount()
    {
        var profile = PriorityProfile.Create(TenantId, "Test", "area-v1", "warn-and-apply-v1", "priority");
        var rule = new PriorityRule(1, "ByDueDate", null);

        profile.AddRule(rule);

        profile.Rules.Should().HaveCount(1);
        profile.Rules[0].RuleName.Should().Be("ByDueDate");
    }

    [Fact]
    public void Rules_AreImmutableFromOutside()
    {
        var profile = PriorityProfile.Create(TenantId, "Test", "area-v1", "warn-and-apply-v1", "fifo");

        var rules = profile.Rules;
        rules.Should().BeAssignableTo<IReadOnlyList<PriorityRule>>("Rules must be read-only");
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => PriorityProfile.Create(TenantId, "", "area-v1", "warn-and-apply-v1", "fifo");
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_WithEmptyCapacityModelId_ShouldThrow()
    {
        var act = () => PriorityProfile.Create(TenantId, "Test", "", "warn-and-apply-v1", "fifo");
        act.Should().Throw<ArgumentException>().WithParameterName("capacityModelId");
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var p1 = PriorityProfile.Create(TenantId, "A", "area-v1", "warn-and-apply-v1", "fifo");
        var p2 = PriorityProfile.Create(TenantId, "B", "area-v1", "warn-and-apply-v1", "fifo");

        p1.Id.Should().NotBe(p2.Id);
    }
}
