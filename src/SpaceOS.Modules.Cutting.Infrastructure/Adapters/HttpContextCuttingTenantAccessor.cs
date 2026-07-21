using Microsoft.AspNetCore.Http;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

public class HttpContextCuttingTenantAccessor : ICuttingTenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCuttingTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var primaryClaim = user?.FindFirst("tid");

            // A present but malformed primary claim must fail closed. Falling back in
            // that case would let a conflicting legacy claim override the wire contract.
            if (primaryClaim is not null)
                return Guid.TryParse(primaryClaim.Value, out var primaryId)
                    ? primaryId
                    : Guid.Empty;

            var legacyClaim = user?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(legacyClaim, out var legacyId)
                ? legacyId
                : Guid.Empty;
        }
    }
}
