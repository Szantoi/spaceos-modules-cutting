namespace SpaceOS.Modules.Cutting.Domain.Enums;

/// <summary>
/// Status of a cutting quote request.
/// </summary>
public enum QuoteStatus
{
    /// <summary>
    /// Quote request submitted, awaiting review by tenant admin.
    /// </summary>
    PendingReview,

    /// <summary>
    /// Quote has been reviewed and approved with a price.
    /// </summary>
    Quoted,

    /// <summary>
    /// Customer accepted the quote and it was converted to an order.
    /// </summary>
    ConvertedToOrder,

    /// <summary>
    /// Quote request was rejected by tenant admin.
    /// </summary>
    Rejected
}
