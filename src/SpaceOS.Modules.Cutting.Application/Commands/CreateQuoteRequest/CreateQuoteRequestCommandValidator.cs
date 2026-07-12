using FluentValidation;
using SpaceOS.Modules.Cutting.Domain.Enums;

namespace SpaceOS.Modules.Cutting.Application.Commands.CreateQuoteRequest;

/// <summary>
/// Validator for CreateQuoteRequestCommand.
/// </summary>
public sealed class CreateQuoteRequestCommandValidator : AbstractValidator<CreateQuoteRequestCommand>
{
    public CreateQuoteRequestCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Data)
            .NotNull().WithMessage("Data is required.");

        When(x => x.Data != null, () =>
        {
            RuleFor(x => x.Data.CustomerEmail)
                .NotEmpty().WithMessage("Customer email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Data.CustomerName)
                .NotEmpty().WithMessage("Customer name is required.")
                .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

            RuleFor(x => x.Data.CustomerPhone)
                .MaximumLength(50).WithMessage("Phone number cannot exceed 50 characters.")
                .When(x => !string.IsNullOrEmpty(x.Data.CustomerPhone));

            RuleFor(x => x.Data.Items)
                .NotEmpty().WithMessage("At least one item is required.")
                .Must(items => items != null && items.Count > 0 && items.Count <= 100)
                .WithMessage("Quote must have between 1 and 100 items.");

            RuleForEach(x => x.Data.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.MaterialType)
                        .NotEmpty().WithMessage("Material type is required.")
                        .Must(BeAValidMaterialType).WithMessage("Invalid material type.");

                    item.RuleFor(x => x.WidthMm)
                        .GreaterThan(0).WithMessage("Width must be greater than 0.")
                        .LessThanOrEqualTo(5000).WithMessage("Width cannot exceed 5000 mm.");

                    item.RuleFor(x => x.HeightMm)
                        .GreaterThan(0).WithMessage("Height must be greater than 0.")
                        .LessThanOrEqualTo(3000).WithMessage("Height cannot exceed 3000 mm.");

                    item.RuleFor(x => x.Quantity)
                        .GreaterThan(0).WithMessage("Quantity must be at least 1.")
                        .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000.");

                    item.RuleFor(x => x.EdgingType)
                        .NotEmpty().WithMessage("Edging type is required.")
                        .Must(BeAValidEdgingType).WithMessage("Invalid edging type.");
                });

            RuleFor(x => x.Data.DeliveryAddress)
                .NotEmpty().WithMessage("Delivery address is required.")
                .MaximumLength(500).WithMessage("Delivery address cannot exceed 500 characters.");

            RuleFor(x => x.Data.RequestedDeliveryDate)
                .Must(date => !date.HasValue || date.Value >= DateTime.UtcNow.Date)
                .WithMessage("Requested delivery date cannot be in the past.")
                .When(x => x.Data.RequestedDeliveryDate.HasValue);
        });
    }

    private static bool BeAValidMaterialType(string materialType)
    {
        return Enum.TryParse<MaterialType>(materialType, out _);
    }

    private static bool BeAValidEdgingType(string edgingType)
    {
        return Enum.TryParse<EdgingType>(edgingType, out _);
    }
}
