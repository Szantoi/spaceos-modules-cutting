using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanNestingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanNestingSnapshots",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    NestingResultJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanNestingSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanNestingSnapshots_CuttingPlanId",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                column: "CuttingPlanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanNestingSnapshots_TenantId",
                schema: "spaceos_cutting",
                table: "PlanNestingSnapshots",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanNestingSnapshots",
                schema: "spaceos_cutting");
        }
    }
}
