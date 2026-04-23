using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDaySlotRefactorDailyPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingJobs_DailyPlans_DailyPlanId",
                schema: "spaceos_cutting",
                table: "CuttingJobs");

            migrationBuilder.DropTable(
                name: "DailyPlans",
                schema: "spaceos_cutting");

            migrationBuilder.RenameColumn(
                name: "DailyPlanId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                newName: "DaySlotId");

            migrationBuilder.RenameIndex(
                name: "IX_CuttingJobs_DailyPlanId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                newName: "IX_CuttingJobs_DaySlotId");

            migrationBuilder.CreateTable(
                name: "DaySlots",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CapacityHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    UsedCapacityHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DaySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DaySlots_CuttingPlans_CuttingPlanId",
                        column: x => x.CuttingPlanId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "CuttingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DaySlots_CuttingPlanId",
                schema: "spaceos_cutting",
                table: "DaySlots",
                column: "CuttingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingJobs_DaySlots_DaySlotId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                column: "DaySlotId",
                principalSchema: "spaceos_cutting",
                principalTable: "DaySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuttingJobs_DaySlots_DaySlotId",
                schema: "spaceos_cutting",
                table: "CuttingJobs");

            migrationBuilder.DropTable(
                name: "DaySlots",
                schema: "spaceos_cutting");

            migrationBuilder.RenameColumn(
                name: "DaySlotId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                newName: "DailyPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_CuttingJobs_DaySlotId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                newName: "IX_CuttingJobs_DailyPlanId");

            migrationBuilder.CreateTable(
                name: "DailyPlans",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AvailableCapacity = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyPlans_CuttingPlans_CuttingPlanId",
                        column: x => x.CuttingPlanId,
                        principalSchema: "spaceos_cutting",
                        principalTable: "CuttingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlans_CuttingPlanId",
                schema: "spaceos_cutting",
                table: "DailyPlans",
                column: "CuttingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_CuttingJobs_DailyPlans_DailyPlanId",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                column: "DailyPlanId",
                principalSchema: "spaceos_cutting",
                principalTable: "DailyPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
