using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.Cutting.Domain.Entities;

namespace SpaceOS.Modules.Cutting.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for PublicQuoteRequest entity (Q3 Track A - MSG-BACKEND-030).
/// </summary>
public class PublicQuoteRequestConfiguration : IEntityTypeConfiguration<PublicQuoteRequest>
{
    public void Configure(EntityTypeBuilder<PublicQuoteRequest> builder)
    {
        builder.ToTable("public_quote_requests", "spaceos_cutting");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .IsRequired();

        builder.Property(q => q.CustomerName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(q => q.CustomerEmail)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(q => q.CustomerPhone)
            .HasMaxLength(20);

        builder.Property(q => q.CompanyName)
            .HasMaxLength(255);

        builder.Property(q => q.Material)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(q => q.LengthMm)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(q => q.WidthMm)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(q => q.ThicknessMm)
            .IsRequired()
            .HasColumnType("decimal(10,2)");

        builder.Property(q => q.Quantity)
            .IsRequired();

        builder.Property(q => q.EdgeType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(q => q.Surface)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(q => q.Urgency)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("standard");

        builder.Property(q => q.Notes)
            .HasColumnType("text");

        builder.Property(q => q.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("received");

        builder.Property(q => q.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(q => q.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Indexes as specified in MSG-BACKEND-030
        builder.HasIndex(q => q.CustomerEmail);
        builder.HasIndex(q => q.CreatedAt).IsDescending();
    }
}
