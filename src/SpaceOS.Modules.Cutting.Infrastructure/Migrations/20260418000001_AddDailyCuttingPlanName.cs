using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

public partial class AddDailyCuttingPlanName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Name",
            schema: "spaceos_cutting",
            table: "DailyCuttingPlans",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Name",
            schema: "spaceos_cutting",
            table: "DailyCuttingPlans");
    }
}
