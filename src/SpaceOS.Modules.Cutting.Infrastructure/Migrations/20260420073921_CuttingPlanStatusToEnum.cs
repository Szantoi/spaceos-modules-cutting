using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CuttingPlanStatusToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "spaceos_cutting",
                table: "DailyCuttingPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Convert existing string values to integer codes via USING clause.
            // Approved → Published(1), InProgress → Frozen(2) per OQ-1 decision.
            migrationBuilder.Sql("""
                ALTER TABLE spaceos_cutting."CuttingPlans"
                    ALTER COLUMN "Status" TYPE integer
                    USING CASE "Status"
                        WHEN 'Draft'       THEN 0
                        WHEN 'Approved'    THEN 1
                        WHEN 'InProgress'  THEN 2
                        WHEN 'Closed'      THEN 3
                        ELSE 0
                    END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "spaceos_cutting",
                table: "DailyCuttingPlans",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                schema: "spaceos_cutting",
                table: "CuttingPlans",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
