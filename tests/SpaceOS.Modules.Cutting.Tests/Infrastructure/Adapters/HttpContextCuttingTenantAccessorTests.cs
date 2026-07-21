using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters;
using Xunit;

namespace SpaceOS.Modules.Cutting.Tests.Infrastructure.Adapters;

public class HttpContextCuttingTenantAccessorTests
{
    [Fact]
    public void TenantId_TidClaimPresent_ReturnsTid()
    {
        var tenantId = Guid.NewGuid();

        var accessor = CreateAccessor(new Claim("tid", tenantId.ToString()));

        accessor.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_LegacyTenantIdClaimPresent_ReturnsLegacyValue()
    {
        var tenantId = Guid.NewGuid();

        var accessor = CreateAccessor(new Claim("tenant_id", tenantId.ToString()));

        accessor.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_BothClaimsPresent_PrefersTid()
    {
        var tenantId = Guid.NewGuid();
        var legacyTenantId = Guid.NewGuid();

        var accessor = CreateAccessor(
            new Claim("tid", tenantId.ToString()),
            new Claim("tenant_id", legacyTenantId.ToString()));

        accessor.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void TenantId_InvalidTidDoesNotFallBack_ReturnsEmpty()
    {
        var accessor = CreateAccessor(
            new Claim("tid", "not-a-guid"),
            new Claim("tenant_id", Guid.NewGuid().ToString()));

        accessor.TenantId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void TenantId_MissingClaims_ReturnsEmpty()
    {
        var accessor = CreateAccessor();

        accessor.TenantId.Should().Be(Guid.Empty);
    }

    private static HttpContextCuttingTenantAccessor CreateAccessor(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        var contextAccessor = new HttpContextAccessor { HttpContext = context };
        return new HttpContextCuttingTenantAccessor(contextAccessor);
    }
}
