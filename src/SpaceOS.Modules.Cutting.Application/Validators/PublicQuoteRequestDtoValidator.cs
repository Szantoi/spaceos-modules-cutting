using FluentValidation;
using SpaceOS.Modules.Cutting.Application.DTOs.QuoteRequest;

namespace SpaceOS.Modules.Cutting.Application.Validators;

/// <summary>
/// Validator for PublicQuoteRequestDto (MSG-BACKEND-079 Phase 5).
/// Validates customer input for public quote requests.
/// </summary>
public sealed class PublicQuoteRequestDtoValidator : AbstractValidator<PublicQuoteRequestDto>
{
    public PublicQuoteRequestDtoValidator()
    {
        // ── Customer Information ──────────────────────────────────────────

        RuleFor(x => x.CustomerName)
            .NotEmpty().WithMessage("Customer name is required")
            .MaximumLength(200).WithMessage("Customer name must not exceed 200 characters");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required")
            .EmailAddress().WithMessage("Invalid email address format")
            .MaximumLength(200).WithMessage("Email must not exceed 200 characters");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(50).WithMessage("Phone number must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CustomerPhone));

        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.CompanyName));

        // ── Material and Dimensions ───────────────────────────────────────

        RuleFor(x => x.Material)
            .NotEmpty().WithMessage("Material is required")
            .MaximumLength(100).WithMessage("Material description must not exceed 100 characters");

        RuleFor(x => x.Dimensions)
            .NotNull().WithMessage("Dimensions are required")
            .ChildRules(dimensions =>
            {
                dimensions.RuleFor(d => d.Length)
                    .InclusiveBetween(1, 10000)
                    .WithMessage("Length must be between 1 and 10,000 mm");

                dimensions.RuleFor(d => d.Width)
                    .InclusiveBetween(1, 10000)
                    .WithMessage("Width must be between 1 and 10,000 mm");

                dimensions.RuleFor(d => d.Thickness)
                    .InclusiveBetween(1, 500)
                    .WithMessage("Thickness must be between 1 and 500 mm");
            });

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Quantity must be at least 1")
            .LessThanOrEqualTo(10000)
            .WithMessage("Quantity must not exceed 10,000");

        // ── Edge and Surface ──────────────────────────────────────────────

        RuleFor(x => x.EdgeType)
            .NotEmpty().WithMessage("Edge type is required")
            .MaximumLength(100).WithMessage("Edge type must not exceed 100 characters");

        RuleFor(x => x.Surface)
            .NotEmpty().WithMessage("Surface is required")
            .MaximumLength(100).WithMessage("Surface description must not exceed 100 characters");

        // ── Urgency ───────────────────────────────────────────────────────

        RuleFor(x => x.Urgency)
            .NotEmpty().WithMessage("Urgency is required")
            .Must(urgency => urgency.ToLowerInvariant() is "standard" or "express")
            .WithMessage("Urgency must be either 'standard' or 'express'");

        // ── Optional Fields ───────────────────────────────────────────────

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2,000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        // ── File Attachments (Phase 5 - optional for now) ────────────────

        RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(a => a.Filename)
                    .NotEmpty().WithMessage("Attachment filename is required")
                    .Must(BeAllowedFileType)
                    .WithMessage("Only .pdf, .jpg, .png, .dxf files are allowed");

                attachment.RuleFor(a => a.Data)
                    .NotEmpty().WithMessage("Attachment data is required")
                    .Must(data => data.Length <= 5 * 1024 * 1024)
                    .WithMessage("File size must not exceed 5 MB");
            })
            .When(x => x.Attachments != null && x.Attachments.Count > 0);
    }

    /// <summary>
    /// Validates that the file extension is in the allowed list.
    /// </summary>
    private static bool BeAllowedFileType(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".dxf";
    }
}
