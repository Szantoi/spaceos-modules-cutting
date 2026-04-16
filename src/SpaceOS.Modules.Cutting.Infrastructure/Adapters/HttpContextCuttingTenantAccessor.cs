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
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}
