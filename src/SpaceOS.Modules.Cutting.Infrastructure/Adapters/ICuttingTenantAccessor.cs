namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters;

public interface ICuttingTenantAccessor
{
    Guid TenantId { get; }
}
