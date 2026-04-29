using Ardalis.Result;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Cutting.Application.Adapters;
using SpaceOS.Modules.Cutting.Contracts.Dtos;
using SpaceOS.Modules.Cutting.Contracts.Providers;
using SpaceOS.Modules.Cutting.Infrastructure.Adapters.Transport;
using SpaceOS.Modules.Inventory.Contracts.Dtos;

namespace SpaceOS.Modules.Cutting.Infrastructure.Adapters.Providers;

/// <summary>
/// External adapter for OptiCut — a file-based XML cutting optimizer.
/// Submits XML via <see cref="FileExchangeTransport"/> and polls for an XML result.
/// GetExecutionStatus and GetWasteReport are not supported by OptiCut.
/// </summary>
internal sealed class OptiCutAdapter : ICuttingProvider
{
    private readonly IExternalAdapterTransport _transport;
    private readonly OptiCutFormatConverter _converter;
    private readonly IAdapterCallAuditWriter _auditWriter;
    private readonly ICuttingTenantAccessor _tenantAccessor;
    private readonly ILogger<OptiCutAdapter> _logger;

    public OptiCutAdapter(
        IExternalAdapterTransport transport,
        OptiCutFormatConverter converter,
        IAdapterCallAuditWriter auditWriter,
        ICuttingTenantAccessor tenantAccessor,
        ILogger<OptiCutAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(converter);
        ArgumentNullException.ThrowIfNull(auditWriter);
        ArgumentNullException.ThrowIfNull(tenantAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _transport = transport;
        _converter = converter;
        _auditWriter = auditWriter;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Guid> SubmitCuttingSheetAsync(CuttingSheetDto sheet, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sheet);

        var callId = Guid.NewGuid();
        var tenantId = _tenantAccessor.TenantId;

        await _auditWriter.RecordSubmitStartedAsync(callId, "opticut", nameof(SubmitCuttingSheetAsync), tenantId, ct)
            .ConfigureAwait(false);

        try
        {
            var payload = _converter.ToVendorInput(sheet);
            var submitResult = await _transport.SubmitAsync(payload, ct).ConfigureAwait(false);

            if (!submitResult.IsSuccess)
            {
                await _auditWriter.RecordFailureAsync(callId, submitResult.Errors, ct).ConfigureAwait(false);
                throw new InvalidOperationException(
                    $"OptiCut transport submit failed: {string.Join("; ", submitResult.Errors)}");
            }

            await _auditWriter.RecordSubmitCompletedAsync(callId, submitResult.Value.CorrelationId, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "OptiCut sheet {SheetId} submitted for tenant {TenantId}, correlationId={CorrelationId}",
                sheet.Id, tenantId, submitResult.Value.CorrelationId);

            return submitResult.Value.SheetId;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            await _auditWriter.RecordExceptionAsync(callId, ex, ct).ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PanelAssignmentDto> GetNestingResultAsync(Guid sheetId, CancellationToken ct = default)
    {
        var correlationId = sheetId.ToString();

        var pollResult = await _transport.PollResultAsync(correlationId, ct).ConfigureAwait(false);

        if (!pollResult.IsSuccess)
        {
            _logger.LogDebug("OptiCut nesting result not ready for sheet {SheetId}", sheetId);
            return new PanelAssignmentDto(sheetId, Array.Empty<PanelPlacementDto>(), 0m, 0);
        }

        var parseResult = _converter.ParseVendorOutput(pollResult.Value, sheetId);

        if (!parseResult.IsSuccess)
        {
            _logger.LogError("OptiCut result parse failed for sheet {SheetId}: {Errors}",
                sheetId, string.Join("; ", parseResult.Errors));
            return new PanelAssignmentDto(sheetId, Array.Empty<PanelPlacementDto>(), 0m, 0);
        }

        return parseResult.Value;
    }

    /// <inheritdoc />
    public Task<CuttingExecutionDto> GetExecutionStatusAsync(Guid sheetId, CancellationToken ct = default)
        => Task.FromResult(new CuttingExecutionDto(sheetId, "NotSupported", null, null, null));

    /// <inheritdoc />
    public Task<WasteReportDto> GetWasteReportAsync(DateRange range, CancellationToken ct = default)
        => Task.FromResult(new WasteReportDto(0m, 0m, 0m, Array.Empty<WasteLineDto>()));
}
