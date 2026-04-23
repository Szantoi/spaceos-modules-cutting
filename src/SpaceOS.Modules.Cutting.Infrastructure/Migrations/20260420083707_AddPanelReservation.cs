using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPanelReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PanelReservations",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DaySlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialCode = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WidthMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    HeightMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanelReservations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PanelReservations_CuttingPlanId",
                schema: "spaceos_cutting",
                table: "PanelReservations",
                column: "CuttingPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PanelReservations_DaySlotId",
                schema: "spaceos_cutting",
                table: "PanelReservations",
                column: "DaySlotId");

            migrationBuilder.CreateIndex(
                name: "IX_PanelReservations_TenantId",
                schema: "spaceos_cutting",
                table: "PanelReservations",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PanelReservations",
                schema: "spaceos_cutting");
        }
    }
}
