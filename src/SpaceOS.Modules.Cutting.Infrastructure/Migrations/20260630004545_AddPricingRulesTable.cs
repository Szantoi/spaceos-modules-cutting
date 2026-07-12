using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingRulesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PricingRules",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductCategory = table.Column<string>(type: "text", nullable: false),
                    BasePricePerUnit = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeadTimeAdjustments",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeadDays = table.Column<int>(type: "integer", nullable: false),
                    AdjustmentFactor = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadTimeAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeadTimeAdjustments_PricingRules_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "PricingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialSurcharges",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    SurchargePercent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialSurcharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialSurcharges_PricingRules_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "PricingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuantityBreakpoints",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuantityBreakpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuantityBreakpoints_PricingRules_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "PricingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeadTimeAdjustments_PricingRuleId",
                schema: "spaceos_cutting",
                table: "LeadTimeAdjustments",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialSurcharges_PricingRuleId",
                schema: "spaceos_cutting",
                table: "MaterialSurcharges",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_QuantityBreakpoints_PricingRuleId",
                schema: "spaceos_cutting",
                table: "QuantityBreakpoints",
                column: "PricingRuleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeadTimeAdjustments",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "MaterialSurcharges",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "QuantityBreakpoints",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "PricingRules",
                schema: "spaceos_cutting");
        }
    }
}
