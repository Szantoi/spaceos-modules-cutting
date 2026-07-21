using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SpaceOS.Modules.Cutting.Tests.Fixtures;

/// <summary>
/// Test authentication handler that processes fake Bearer tokens for integration tests.
/// Extracts userId from the token and test-only tenant claims from request headers.
/// </summary>
internal class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer fake-token-", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Extract userId from token (format: "Bearer fake-token-{userId}")
        var token = authHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
        var userIdStr = token.Replace("fake-token-", "", StringComparison.OrdinalIgnoreCase);

        if (!Guid.TryParse(userIdStr, out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid test token format"));
        }

        // Create claims identity. The API is responsible for validating claim values.
        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, $"TestUser-{userId}"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        if (Request.Headers.TryGetValue("X-Test-Tid", out var canonicalTenantClaim))
            claims.Add(new Claim("tid", canonicalTenantClaim.ToString()));

        if (Request.Headers.TryGetValue("X-Test-Legacy-Tenant-Id", out var legacyTenantClaim))
            claims.Add(new Claim("tenant_id", legacyTenantClaim.ToString()));

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
