using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Cutting.Domain.Interfaces;

namespace SpaceOS.Modules.Cutting.Application.Commands.RejectQuote;

/// <summary>
/// Handler for rejecting a quote request.
/// </summary>
public sealed class RejectQuoteCommandHandler : IRequestHandler<RejectQuoteCommand, Result>
{
    private readonly IQuoteRequestRepository _repository;
    private readonly ICuttingRepository _cuttingRepository;

    public RejectQuoteCommandHandler(
        IQuoteRequestRepository repository,
        ICuttingRepository cuttingRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cuttingRepository = cuttingRepository ?? throw new ArgumentNullException(nameof(cuttingRepository));
    }

    public async Task<Result> Handle(RejectQuoteCommand request, CancellationToken ct)
    {
        var quote = await _repository.GetByIdAsync(request.QuoteId, ct).ConfigureAwait(false);
        if (quote == null)
        {
            return Result.NotFound($"Quote request {request.QuoteId} not found.");
        }

        try
        {
            quote.Reject(request.Reason, request.UserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Error(ex.Message);
        }

        await _repository.UpdateAsync(quote, ct).ConfigureAwait(false);
        await _cuttingRepository.SaveChangesAsync(ct).ConfigureAwait(false);

        // TODO: Send email notification to customer

        return Result.Success();
    }
}
