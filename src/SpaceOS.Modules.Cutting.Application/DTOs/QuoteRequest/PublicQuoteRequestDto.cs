using System.ComponentModel.DataAnnotations;

namespace SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

/// <summary>
/// DTO for public quote request API (Q3 Track A - MSG-BACKEND-030).
/// Single-item quote request format for B2C customers.
/// </summary>
public sealed record PublicQuoteRequestDto
{
    [Required]
    public required string CustomerName { get; init; }

    [Required]
    [EmailAddress]
    public required string CustomerEmail { get; init; }

    public string? CustomerPhone { get; init; }

    public string? CompanyName { get; init; }

    [Required]
    public required string Material { get; init; }

    [Required]
    public required DimensionsDto Dimensions { get; init; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public required int Quantity { get; init; }

    [Required]
    public required string EdgeType { get; init; }

    [Required]
    public required string Surface { get; init; }

    [Required]
    public string Urgency { get; init; } = "standard";

    public string? Notes { get; init; }

    public List<AttachmentDto>? Attachments { get; init; }
}

/// <summary>
/// Panel dimensions in millimeters.
/// </summary>
public sealed record DimensionsDto
{
    [Required]
    [Range(1, 10000, ErrorMessage = "Length must be between 1 and 10000 mm")]
    public required decimal Length { get; init; }

    [Required]
    [Range(1, 10000, ErrorMessage = "Width must be between 1 and 10000 mm")]
    public required decimal Width { get; init; }

    [Required]
    [Range(1, 500, ErrorMessage = "Thickness must be between 1 and 500 mm")]
    public required decimal Thickness { get; init; }
}

/// <summary>
/// File attachment (base64 encoded).
/// </summary>
public sealed record AttachmentDto
{
    [Required]
    public required string Filename { get; init; }

    [Required]
    public required string Data { get; init; } // base64 encoded
}

/// <summary>
/// Response DTO for public quote request creation.
/// </summary>
public sealed record PublicQuoteResponseDto
{
    public required Guid QuoteId { get; init; }
    public required string Status { get; init; } // "received"
    public required string EstimatedReplyTime { get; init; } // "2 business days"
    public required string TrackingUrl { get; init; } // "/public/quote/{quoteId}/status"
}
