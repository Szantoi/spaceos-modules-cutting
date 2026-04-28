using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanNestingSnapshotEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Algorithm",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlacementsJson",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<long>(
                name: "WasteAreaMm2",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<decimal>(
                name: "YieldPercent",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                type: "numeric(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Algorithm",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots");

            migrationBuilder.DropColumn(
                name: "PlacementsJson",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots");

            migrationBuilder.DropColumn(
                name: "WasteAreaMm2",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots");

            migrationBuilder.DropColumn(
                name: "YieldPercent",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots");
        }
    }
}
