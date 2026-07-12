using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace SpaceOS.Modules.Cutting.Tests.Fixtures;

/// <summary>
/// Test authentication handler that processes fake Bearer tokens for integration tests.
/// Extracts tenantId and userId from the token format: "Bearer fake-token-{userId}".
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

        // Create claims identity
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, $"TestUser-{userId}"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
