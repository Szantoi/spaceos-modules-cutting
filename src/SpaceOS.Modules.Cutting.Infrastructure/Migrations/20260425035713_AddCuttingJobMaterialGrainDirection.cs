using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCuttingJobMaterialGrainDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GrainDirection",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Material",
                schema: "spaceos_cutting",
                table: "CuttingJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrainDirection",
                schema: "spaceos_cutting",
                table: "CuttingJobs");

            migrationBuilder.DropColumn(
                name: "Material",
                schema: "spaceos_cutting",
                table: "CuttingJobs");
        }
    }
}
