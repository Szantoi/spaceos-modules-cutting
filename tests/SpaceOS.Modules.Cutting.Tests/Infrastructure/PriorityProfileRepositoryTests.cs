using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Infrastructure.Persistence;
using SpaceOS.Modules.Cutting.Infrastructure.Repositories;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure;

public class PriorityProfileRepositoryTests : IDisposable
{
    private readonly CuttingDbContext _db;
    private readonly PriorityProfileRepository _repo;
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    public PriorityProfileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CuttingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new CuttingDbContext(options);
        _repo = new PriorityProfileRepository(_db);
    }

    private static PriorityProfile MakeProfile(Guid? tenantId, string name, bool isDefault = false)
        => PriorityProfile.Create(tenantId, name, "area-v1", "warn-and-apply-v1", "maxcut-v1", isDefault);

    [Fact]
    public async Task AddAsync_ThenGetByTenant_ReturnsProfile()
    {
        var profile = MakeProfile(TenantA, "Test");
        await _repo.AddAsync(profile);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByTenantAsync(TenantA);

        result.Should().ContainSingle(p => p.Name == "Test");
    }

    [Fact]
    public async Task GetByTenant_IncludesGlobalPresets()
    {
        var globalPreset = MakeProfile(null, "GlobalPreset");
        var tenantProfile = MakeProfile(TenantA, "TenantProfile");
        await _repo.AddAsync(globalPreset);
        await _repo.AddAsync(tenantProfile);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByTenantAsync(TenantA);

        result.Should().HaveCount(2, "global preset + tenant-specific profile");
    }

    [Fact]
    public async Task GetByTenant_DoesNotReturnOtherTenantsProfiles()
    {
        var profileA = MakeProfile(TenantA, "ForA");
        var profileB = MakeProfile(TenantB, "ForB");
        await _repo.AddAsync(profileA);
        await _repo.AddAsync(profileB);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByTenantAsync(TenantA);

        result.Should().ContainSingle(p => p.Name == "ForA");
        result.Should().NotContain(p => p.Name == "ForB");
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsDefaultProfile()
    {
        var defaultProfile = MakeProfile(TenantA, "Default", isDefault: true);
        var otherProfile  = MakeProfile(TenantA, "Other",   isDefault: false);
        await _repo.AddAsync(defaultProfile);
        await _repo.AddAsync(otherProfile);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetDefaultAsync(TenantA);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Default");
    }

    [Fact]
    public async Task GetGlobalPresetsAsync_ReturnsOnlyGlobalProfiles()
    {
        var global = MakeProfile(null, "GlobalA");
        var tenant = MakeProfile(TenantA, "TenantX");
        await _repo.AddAsync(global);
        await _repo.AddAsync(tenant);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetGlobalPresetsAsync();

        result.Should().ContainSingle(p => p.Name == "GlobalA");
        result.Should().NotContain(p => p.Name == "TenantX");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectProfile()
    {
        var profile = MakeProfile(TenantA, "ById");
        await _repo.AddAsync(profile);
        await _repo.SaveChangesAsync();

        var result = await _repo.GetByIdAsync(profile.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("ById");
    }

    public void Dispose() => _db.Dispose();
}
