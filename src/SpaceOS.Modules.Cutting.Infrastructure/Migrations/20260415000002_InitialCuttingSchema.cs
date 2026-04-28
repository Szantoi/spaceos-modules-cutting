using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

public partial class InitialCuttingSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "spaceos_cutting");

        migrationBuilder.CreateTable(
            name: "CuttingSheets",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                OrderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CuttingSheets", x => x.Id));

        migrationBuilder.CreateTable(
            name: "CuttingLines",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CuttingSheetId = table.Column<Guid>(type: "uuid", nullable: false),
                PartName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                WidthMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                HeightMm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                ThicknessMm = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CuttingLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_CuttingLines_CuttingSheets_CuttingSheetId",
                    column: x => x.CuttingSheetId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "CuttingSheets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DailyCuttingPlans",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DailyCuttingPlans", x => x.Id));

        migrationBuilder.CreateTable(
            name: "CuttingBatches",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DailyCuttingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ThicknessMm = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: false),
                SheetIds = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CuttingBatches", x => x.Id);
                table.ForeignKey(
                    name: "FK_CuttingBatches_DailyCuttingPlans_DailyCuttingPlanId",
                    column: x => x.DailyCuttingPlanId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "DailyCuttingPlans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CuttingExecutions",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CuttingSheetId = table.Column<Guid>(type: "uuid", nullable: false),
                AssignedTo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                WasteAreaCm2 = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CuttingExecutions", x => x.Id));

        // Indexes
        migrationBuilder.CreateIndex("IX_CuttingSheets_TenantId", "CuttingSheets", "TenantId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingSheets_TenantId_Status", "CuttingSheets", new[] { "TenantId", "Status" }, schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingLines_CuttingSheetId", "CuttingLines", "CuttingSheetId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_DailyCuttingPlans_TenantId", "DailyCuttingPlans", "TenantId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_DailyCuttingPlans_TenantId_PlanDate", "DailyCuttingPlans", new[] { "TenantId", "PlanDate" }, schema: "spaceos_cutting", unique: true);
        migrationBuilder.CreateIndex("IX_CuttingBatches_DailyCuttingPlanId", "CuttingBatches", "DailyCuttingPlanId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingExecutions_TenantId", "CuttingExecutions", "TenantId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingExecutions_CuttingSheetId", "CuttingExecutions", "CuttingSheetId", schema: "spaceos_cutting", unique: true);
        migrationBuilder.CreateIndex("IX_CuttingExecutions_TenantId_CompletedAt", "CuttingExecutions", new[] { "TenantId", "CompletedAt" }, schema: "spaceos_cutting");

        // RLS
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.""CuttingSheets"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""CuttingSheets"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""CuttingSheets""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);

ALTER TABLE spaceos_cutting.""DailyCuttingPlans"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""DailyCuttingPlans"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""DailyCuttingPlans""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);

ALTER TABLE spaceos_cutting.""CuttingExecutions"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""CuttingExecutions"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""CuttingExecutions""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""CuttingSheets"";
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""DailyCuttingPlans"";
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""CuttingExecutions"";
");
        migrationBuilder.DropTable(name: "CuttingExecutions", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "CuttingBatches", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "DailyCuttingPlans", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "CuttingLines", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "CuttingSheets", schema: "spaceos_cutting");
    }
}
