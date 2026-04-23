using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingJobDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HeightMm",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthMm",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeightMm",
                schema: "spaceos_cutting",
                table: "CuttingJobs");

            migrationBuilder.DropColumn(
                name: "WidthMm",
                schema: "spaceos_cutting",
                table: "CuttingJobs");
        }
    }
}
