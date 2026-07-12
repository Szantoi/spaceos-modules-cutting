using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Aggregates;
using SpaceOS.Modules.Cutting.Domain.Enums;
using SpaceOS.Modules.Cutting.Domain.ValueObjects;
using System.Text.Json;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

public class CuttingQuoteRequestConfiguration : IEntityTypeConfiguration<CuttingQuoteRequest>
{
    public void Configure(EntityTypeBuilder<CuttingQuoteRequest> builder)
    {
        builder.ToTable("quote_requests", "spaceos_cutting");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .IsRequired();

        builder.Property(q => q.TenantId)
            .IsRequired();

        builder.Property(q => q.QuoteNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(q => q.QuoteNumber)
            .IsUnique();

        builder.Property(q => q.TrackingToken)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(q => q.TrackingToken)
            .IsUnique();

        // ContactInfo as owned type (stored as columns)
        builder.OwnsOne(q => q.CustomerContact, contact =>
        {
            contact.Property(c => c.Email)
                .HasColumnName("customer_email")
                .IsRequired()
                .HasMaxLength(200);

            contact.Property(c => c.Name)
                .HasColumnName("customer_name")
                .IsRequired()
                .HasMaxLength(200);

            contact.Property(c => c.Phone)
                .HasColumnName("customer_phone")
                .HasMaxLength(50);
        });

        // Items as JSON column
        builder.Property(q => q.Items)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<QuoteLineItem>>(json, (JsonSerializerOptions?)null) ?? new List<QuoteLineItem>())
            .HasColumnType("jsonb")
            .HasColumnName("items");

        // DeliveryDetails as owned type
        builder.OwnsOne(q => q.Delivery, delivery =>
        {
            delivery.Property(d => d.Address)
                .HasColumnName("delivery_address")
                .IsRequired()
                .HasMaxLength(500);

            delivery.Property(d => d.RequestedDate)
                .HasColumnName("requested_delivery_date");
        });

        builder.Property(q => q.Status)
            .IsRequired()
            .HasConversion<string>();

        // QuotedPrice as owned type (nullable)
        builder.OwnsOne(q => q.QuotedPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("quoted_price_amount")
                .HasColumnType("decimal(10,2)");

            price.Property(p => p.Currency)
                .HasColumnName("quoted_price_currency")
                .HasMaxLength(3);
        });

        builder.Property(q => q.ReviewedAt);

        builder.Property(q => q.ReviewedByUserId);

        builder.Property(q => q.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(q => q.ConvertedToOrderAt);

        builder.Property(q => q.CuttingSheetId);

        builder.HasIndex(q => q.CuttingSheetId);

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.UpdatedAt)
            .IsRequired();

        builder.Property(q => q.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => new { q.TenantId, q.Status });
        builder.HasIndex(q => q.CreatedAt);

        // Ignore domain events (not persisted)
        builder.Ignore(q => q.DomainEvents);
    }
}
