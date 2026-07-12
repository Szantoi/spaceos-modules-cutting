using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicQuoteRequestTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "public_quote_requests",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CompanyName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Material = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LengthMm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    WidthMm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    ThicknessMm = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    EdgeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Surface = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "standard"),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "received"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_public_quote_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplexityModifiers",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifierType = table.Column<string>(type: "text", nullable: false),
                    MultiplierValue = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexityModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplexityModifiers_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialPricings",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialType = table.Column<string>(type: "text", nullable: false),
                    PricePerSquareMeter = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialPricings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialPricings_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplexityModifiers_PriceListId",
                schema: "spaceos_cutting",
                table: "ComplexityModifiers",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPricings_PriceListId",
                schema: "spaceos_cutting",
                table: "MaterialPricings",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_public_quote_requests_CreatedAt",
                schema: "spaceos_cutting",
                table: "public_quote_requests",
                column: "CreatedAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_public_quote_requests_CustomerEmail",
                schema: "spaceos_cutting",
                table: "public_quote_requests",
                column: "CustomerEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplexityModifiers",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "MaterialPricings",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "public_quote_requests",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "PriceLists",
                schema: "spaceos_cutting");
        }
    }
}
