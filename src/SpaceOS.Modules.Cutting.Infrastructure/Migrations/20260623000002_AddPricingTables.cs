using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Q3 Track B — Adds pricing tables for automated quote pricing.
/// </summary>
/// <remarks>
/// Creates three tables:
/// - PriceLists: Price configurations per tenant with validity periods
/// - MaterialPricing: Material-specific pricing (price per m²)
/// - ComplexityModifiers: Complexity multipliers for cuts and shapes
///
/// Includes seed data for Doorstar default price list.
/// </remarks>
public partial class AddPricingTables : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // PriceLists table
        migrationBuilder.CreateTable(
            name: "PriceLists",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PriceLists", x => x.Id);
            });

        // MaterialPricing table
        migrationBuilder.CreateTable(
            name: "MaterialPricing",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialType = table.Column<string>(type: "text", nullable: false),
                PricePerSquareMeter = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                Currency = table.Column<string>(type: "text", nullable: false, defaultValue: "HUF")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MaterialPricing", x => x.Id);
                table.ForeignKey(
                    name: "FK_MaterialPricing_PriceLists",
                    column: x => x.PriceListId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "PriceLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // ComplexityModifiers table
        migrationBuilder.CreateTable(
            name: "ComplexityModifiers",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                ModifierType = table.Column<string>(type: "text", nullable: false),
                MultiplierValue = table.Column<decimal>(type: "numeric(5,3)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplexityModifiers", x => x.Id);
                table.ForeignKey(
                    name: "FK_ComplexityModifiers_PriceLists",
                    column: x => x.PriceListId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "PriceLists",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Indexes
        migrationBuilder.CreateIndex(
            name: "IX_PriceLists_TenantId",
            schema: "spaceos_cutting",
            table: "PriceLists",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_PriceLists_IsActive",
            schema: "spaceos_cutting",
            table: "PriceLists",
            column: "IsActive");

        migrationBuilder.CreateIndex(
            name: "IX_MaterialPricing_PriceListId",
            schema: "spaceos_cutting",
            table: "MaterialPricing",
            column: "PriceListId");

        migrationBuilder.CreateIndex(
            name: "IX_ComplexityModifiers_PriceListId",
            schema: "spaceos_cutting",
            table: "ComplexityModifiers",
            column: "PriceListId");

        // Seed data: Default price list for Doorstar tenant
        migrationBuilder.Sql("""
            -- Insert default price list for Doorstar
            DO $$
            DECLARE
                v_doorstar_tenant_id uuid;
                v_pricelist_id uuid;
            BEGIN
                -- Get Doorstar tenant ID
                SELECT "Id" INTO v_doorstar_tenant_id
                FROM "Tenants"
                WHERE "Subdomain" = 'doorstar'
                LIMIT 1;

                IF v_doorstar_tenant_id IS NOT NULL THEN
                    v_pricelist_id := gen_random_uuid();

                    -- Insert price list
                    INSERT INTO spaceos_cutting."PriceLists" ("Id", "TenantId", "Name", "EffectiveFrom", "IsActive", "CreatedAt")
                    VALUES (v_pricelist_id, v_doorstar_tenant_id, 'Default 2026', '2026-01-01 00:00:00+00', true, now());

                    -- Insert material pricing
                    INSERT INTO spaceos_cutting."MaterialPricing" ("Id", "PriceListId", "MaterialType", "PricePerSquareMeter", "Currency")
                    VALUES
                        (gen_random_uuid(), v_pricelist_id, 'MDF', 4000.00, 'HUF'),
                        (gen_random_uuid(), v_pricelist_id, 'Plywood', 6500.00, 'HUF'),
                        (gen_random_uuid(), v_pricelist_id, 'Chipboard', 3500.00, 'HUF'),
                        (gen_random_uuid(), v_pricelist_id, 'OSB', 5000.00, 'HUF'),
                        (gen_random_uuid(), v_pricelist_id, 'Laminated', 8500.00, 'HUF');

                    -- Insert complexity modifiers
                    INSERT INTO spaceos_cutting."ComplexityModifiers" ("Id", "PriceListId", "ModifierType", "MultiplierValue")
                    VALUES
                        (gen_random_uuid(), v_pricelist_id, 'CutCount', 0.100),
                        (gen_random_uuid(), v_pricelist_id, 'ShapeComplexity', 0.150),
                        (gen_random_uuid(), v_pricelist_id, 'EdgeBanding', 0.200);
                END IF;
            END $$;
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ComplexityModifiers",
            schema: "spaceos_cutting");

        migrationBuilder.DropTable(
            name: "MaterialPricing",
            schema: "spaceos_cutting");

        migrationBuilder.DropTable(
            name: "PriceLists",
            schema: "spaceos_cutting");
    }
}
