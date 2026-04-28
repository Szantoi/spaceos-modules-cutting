using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

public partial class AddCuttingPlanAggregate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CuttingPlans",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PlanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PlanDays = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StrategyId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CuttingPlans", x => x.Id));

        migrationBuilder.CreateTable(
            name: "DailyPlans",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AvailableCapacity = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false)
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

        migrationBuilder.CreateTable(
            name: "CuttingJobs",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DailyPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                EstimatedTimeHours = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CuttingJobs", x => x.Id);
                table.ForeignKey(
                    name: "FK_CuttingJobs_DailyPlans_DailyPlanId",
                    column: x => x.DailyPlanId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "DailyPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_CuttingPlans_TenantId", "CuttingPlans", "TenantId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingPlans_TenantId_PlanDate", "CuttingPlans", new[] { "TenantId", "PlanDate" }, schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_DailyPlans_CuttingPlanId", "DailyPlans", "CuttingPlanId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingJobs_DailyPlanId", "CuttingJobs", "DailyPlanId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingJobs_OrderId", "CuttingJobs", "OrderId", schema: "spaceos_cutting");

        // RLS: CuttingPlan is tenant-specific
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.""CuttingPlans"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""CuttingPlans"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""CuttingPlans""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""CuttingPlans"";
");
        migrationBuilder.DropTable(name: "CuttingJobs", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "DailyPlans", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "CuttingPlans", schema: "spaceos_cutting");
    }
}
