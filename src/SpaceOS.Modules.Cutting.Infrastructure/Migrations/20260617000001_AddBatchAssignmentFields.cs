using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Adds BatchId and Priority columns to CuttingExecutions table
/// and BatchAssignments table for the assign-batch endpoint (TOP 3 dependency).
/// Includes unique constraint on (BatchId, ScheduleDate) for idempotency.
/// </summary>
public partial class AddBatchAssignmentFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add BatchId column to CuttingExecutions table (nullable for existing records)
        migrationBuilder.AddColumn<Guid>(
            name: "BatchId",
            schema: "spaceos_cutting",
            table: "CuttingExecutions",
            type: "uuid",
            nullable: true);

        // Add Priority column to CuttingExecutions table (nullable for existing records)
        migrationBuilder.AddColumn<int>(
            name: "Priority",
            schema: "spaceos_cutting",
            table: "CuttingExecutions",
            type: "integer",
            nullable: true);

        // Create BatchAssignments table
        migrationBuilder.CreateTable(
            name: "BatchAssignments",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                PlanDate = table.Column<DateOnly>(type: "date", nullable: false),
                MachineId = table.Column<Guid>(type: "uuid", nullable: false),
                OperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                Priority = table.Column<int>(type: "integer", nullable: false),
                StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BatchAssignments", x => x.Id);
            });

        // Add unique constraint on (BatchId, PlanDate) for idempotency
        migrationBuilder.CreateIndex(
            name: "IX_BatchAssignments_BatchId_PlanDate",
            schema: "spaceos_cutting",
            table: "BatchAssignments",
            columns: new[] { "BatchId", "PlanDate" },
            unique: true);

        // Add index on TenantId for RLS filtering
        migrationBuilder.CreateIndex(
            name: "IX_BatchAssignments_TenantId",
            schema: "spaceos_cutting",
            table: "BatchAssignments",
            column: "TenantId");

        // Add index on ExecutionId for lookups
        migrationBuilder.CreateIndex(
            name: "IX_BatchAssignments_ExecutionId",
            schema: "spaceos_cutting",
            table: "BatchAssignments",
            column: "ExecutionId");

        // Enable RLS on BatchAssignments table
        migrationBuilder.Sql(@"
            ALTER TABLE spaceos_cutting.""BatchAssignments"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE spaceos_cutting.""BatchAssignments"" FORCE ROW LEVEL SECURITY;
        ");

        // Create RLS policy for BatchAssignments
        migrationBuilder.Sql(@"
            CREATE POLICY tenant_isolation_policy ON spaceos_cutting.""BatchAssignments""
            USING (""TenantId"" = current_setting('app.current_tenant_id', TRUE)::uuid);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop RLS policy
        migrationBuilder.Sql(@"DROP POLICY IF EXISTS tenant_isolation_policy ON spaceos_cutting.""BatchAssignments"";");

        // Drop BatchAssignments table
        migrationBuilder.DropTable(
            name: "BatchAssignments",
            schema: "spaceos_cutting");

        // Drop columns from CuttingExecutions
        migrationBuilder.DropColumn(
            name: "BatchId",
            schema: "spaceos_cutting",
            table: "CuttingExecutions");

        migrationBuilder.DropColumn(
            name: "Priority",
            schema: "spaceos_cutting",
            table: "CuttingExecutions");
    }
}
