using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Entities;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.AcceptQuote;

/// <summary>
/// Handler for accepting a quote (converts to CuttingSheet order).
/// </summary>
public sealed class AcceptQuoteCommandHandler : IRequestHandler<AcceptQuoteCommand, Result>
{
    private readonly IQuoteRequestRepository _quoteRepository;
    private readonly ICuttingRepository _cuttingRepository;

    public AcceptQuoteCommandHandler(
        IQuoteRequestRepository quoteRepository,
        ICuttingRepository cuttingRepository)
    {
        _quoteRepository = quoteRepository ?? throw new ArgumentNullException(nameof(quoteRepository));
        _cuttingRepository = cuttingRepository ?? throw new ArgumentNullException(nameof(cuttingRepository));
    }

    public async Task<Result> Handle(AcceptQuoteCommand request, CancellationToken ct)
    {
        var quote = await _quoteRepository.GetByTrackingTokenAsync(request.TrackingToken, ct)
            .ConfigureAwait(false);

        if (quote == null)
        {
            return Result.NotFound($"Quote with tracking token {request.TrackingToken} not found.");
        }

        if (quote.Status != QuoteStatus.Quoted)
        {
            return Result.Error($"Quote is not in Quoted status (current: {quote.Status}).");
        }

        // Create CuttingSheet from quote items
        var lines = quote.Items.Select(item => CuttingLine.Create(
            Guid.Empty, // Will be set by CuttingSheet.Create
            $"{item.Material}_{item.WidthMm}x{item.HeightMm}",
            item.Material.ToString(),
            item.WidthMm,
            item.HeightMm,
            GetThicknessFromMaterial(item.Material.ToString()),
            item.Quantity,
            item.Notes ?? string.Empty)).ToList();

        var cuttingSheet = CuttingSheet.Create(
            quote.TenantId,
            quote.QuoteNumber,
            lines);

        await _cuttingRepository.AddCuttingSheetAsync(cuttingSheet, ct).ConfigureAwait(false);

        // Update quote status
        try
        {
            quote.ConvertToOrder(cuttingSheet.Id);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Error(ex.Message);
        }

        await _quoteRepository.UpdateAsync(quote, ct).ConfigureAwait(false);
        await _cuttingRepository.SaveChangesAsync(ct).ConfigureAwait(false);

        return Result.Success();
    }

    private static int GetThicknessFromMaterial(string materialType)
    {
        if (materialType.Contains("18MM", StringComparison.OrdinalIgnoreCase))
            return 18;
        if (materialType.Contains("22MM", StringComparison.OrdinalIgnoreCase))
            return 22;
        return 18; // default
    }
}
