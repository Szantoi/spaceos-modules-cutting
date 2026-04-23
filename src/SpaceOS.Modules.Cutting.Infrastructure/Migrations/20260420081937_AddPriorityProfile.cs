using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorityProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriorityProfiles",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CapacityModelId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReworkPolicyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PlanningStrategyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Rules = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriorityProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriorityProfiles_TenantId",
                schema: "spaceos_cutting",
                table: "PriorityProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PriorityProfiles_TenantId_IsDefault",
                schema: "spaceos_cutting",
                table: "PriorityProfiles",
                columns: new[] { "TenantId", "IsDefault" });

            // Seed: global presets (TenantId = NULL — visible to all tenants)
            migrationBuilder.InsertData(
                schema: "spaceos_cutting",
                table: "PriorityProfiles",
                columns: new[] { "Id", "TenantId", "Name", "IsDefault", "CapacityModelId", "ReworkPolicyId", "PlanningStrategyId", "CreatedAt", "Rules" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000001"),
                    null,
                    "Manufacturer",
                    true,
                    "area-v1",
                    "warn-and-apply-v1",
                    "fifo",
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    "[]"
                });

            migrationBuilder.InsertData(
                schema: "spaceos_cutting",
                table: "PriorityProfiles",
                columns: new[] { "Id", "TenantId", "Name", "IsDefault", "CapacityModelId", "ReworkPolicyId", "PlanningStrategyId", "CreatedAt", "Rules" },
                values: new object[]
                {
                    new Guid("00000000-0000-0000-0000-000000000002"),
                    null,
                    "PanelCutter",
                    false,
                    "area-v1",
                    "warn-and-apply-v1",
                    "maxcut-v1",
                    new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
                    "[]"
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriorityProfiles",
                schema: "spaceos_cutting");
        }
    }
}
