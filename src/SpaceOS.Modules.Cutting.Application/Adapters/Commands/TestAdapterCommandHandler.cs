using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Application.Adapters.Dtos;

namespace SpaceOS.Modules.Cutting.Application.Adapters.Commands;

/// <summary>
/// Handles <see cref="TestAdapterCommand"/>.
/// Resolves the configured adapter for the tenant and attempts a lightweight connectivity check.
/// The builtin adapter and manual adapter always report healthy.
/// External adapters (OptiCut, CutRite) are resolved and a simple call is attempted.
/// </summary>
public sealed class TestAdapterCommandHandler : IRequestHandler<TestAdapterCommand, Result<AdapterTestResultDto>>
{
    private readonly ITenantCuttingProviderConfigRepository _configRepository;
    private readonly IAdapterFactory _adapterFactory;

    public TestAdapterCommandHandler(
        ITenantCuttingProviderConfigRepository configRepository,
        IAdapterFactory adapterFactory)
    {
        ArgumentNullException.ThrowIfNull(configRepository);
        ArgumentNullException.ThrowIfNull(adapterFactory);

        _configRepository = configRepository;
        _adapterFactory = adapterFactory;
    }

    /// <inheritdoc />
    public async Task<Result<AdapterTestResultDto>> Handle(TestAdapterCommand request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var config = await _configRepository.GetByTenantAsync(request.TenantId, ct).ConfigureAwait(false);
        var adapterName = (config is { IsEnabled: true }) ? config.AdapterName : "builtin";

        if (!_adapterFactory.RegisteredAdapterNames.Contains(adapterName))
        {
            return Result<AdapterTestResultDto>.Success(
                new AdapterTestResultDto(false, $"Adapter '{adapterName}' is not registered."));
        }

        // Builtin and manual adapters are always healthy — no external dependency to ping
        if (adapterName is "builtin" or "manual")
        {
            return Result<AdapterTestResultDto>.Success(
                new AdapterTestResultDto(true, $"Adapter '{adapterName}' is healthy."));
        }

        // For external adapters attempt to call GetNestingResultAsync with an empty GUID.
        // A "not found" style response means the transport is reachable; an exception means it is not.
        var provider = _adapterFactory.GetByName(adapterName);
        try
        {
            await provider.GetNestingResultAsync(Guid.Empty, ct).ConfigureAwait(false);
            return Result<AdapterTestResultDto>.Success(
                new AdapterTestResultDto(true, $"Adapter '{adapterName}' responded."));
        }
        catch (Exception ex)
        {
            return Result<AdapterTestResultDto>.Success(
                new AdapterTestResultDto(false, $"Adapter '{adapterName}' unreachable: {ex.Message}"));
        }
    }
}
