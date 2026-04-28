using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 4 migration: replaces the Phase 3 CuttingExecutions stub with the full aggregate schema.
/// Adds ProgressEvents, OffcutReports, MilestoneSubscriptions, and OutboxMessages tables.
/// All tables have RLS FORCE for tenant isolation.
/// ProgressEvents has an append-only trigger to prevent deletes.
/// </summary>
public partial class AddCuttingExecutionAggregate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Drop Phase 3 stub table (different schema, incompatible columns)
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""CuttingExecutions"";
ALTER TABLE IF EXISTS spaceos_cutting.""CuttingExecutions"" DISABLE ROW LEVEL SECURITY;
");
        migrationBuilder.DropTable(
            name: "CuttingExecutions",
            schema: "spaceos_cutting");

        // 2. Create Phase 4 CuttingExecutions table
        migrationBuilder.CreateTable(
            name: "CuttingExecutions",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                SheetId = table.Column<Guid>(type: "uuid", nullable: false),
                MachineId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                TotalPanels = table.Column<int>(type: "integer", nullable: false),
                PanelsCompleted = table.Column<int>(type: "integer", nullable: false),
                ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelReason = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                OffcutAreaMm2 = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                TotalAreaMm2 = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                WorkerConsentActive = table.Column<bool>(type: "boolean", nullable: false),
                // WorkerAssignment owned VO
                WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                EnrollmentId = table.Column<Guid>(type: "uuid", nullable: true),
                // ScheduleWindow owned VO
                WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                // CompletionProof optional owned VO
                ProofLevel = table.Column<int>(type: "integer", nullable: true),
                ProofHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                ProofSignature = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                ProofBlobRef = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                ProofEncryptedWith = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_CuttingExecutions", x => x.Id));

        // 3. Indexes on CuttingExecutions
        migrationBuilder.CreateIndex(
            name: "IX_CuttingExecutions_TenantId",
            schema: "spaceos_cutting",
            table: "CuttingExecutions",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_CuttingExecutions_SheetId",
            schema: "spaceos_cutting",
            table: "CuttingExecutions",
            column: "SheetId");

        migrationBuilder.CreateIndex(
            name: "IX_CuttingExecutions_Status",
            schema: "spaceos_cutting",
            table: "CuttingExecutions",
            column: "Status");

        // 4. ProgressEvents table (append-only)
        migrationBuilder.CreateTable(
            name: "ProgressEvents",
            schema: "spaceos_cutting",
            columns: table => new
            {
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                Panel = table.Column<int>(type: "integer", nullable: true),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EventHmac = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProgressEvents", x => new { x.ExecutionId, x.EventId });
                table.ForeignKey(
                    name: "FK_ProgressEvents_CuttingExecutions_ExecutionId",
                    column: x => x.ExecutionId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "CuttingExecutions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ProgressEvents_ExecutionId",
            schema: "spaceos_cutting",
            table: "ProgressEvents",
            column: "ExecutionId");

        migrationBuilder.CreateIndex(
            name: "IX_ProgressEvents_EventId",
            schema: "spaceos_cutting",
            table: "ProgressEvents",
            column: "EventId",
            unique: true);

        // 5. OffcutReports table
        migrationBuilder.CreateTable(
            name: "OffcutReports",
            schema: "spaceos_cutting",
            columns: table => new
            {
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                OffcutId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                WidthMm = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                HeightMm = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                AreaMm2 = table.Column<decimal>(type: "numeric(14,4)", precision: 14, scale: 4, nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OffcutReports", x => new { x.ExecutionId, x.OffcutId });
                table.ForeignKey(
                    name: "FK_OffcutReports_CuttingExecutions_ExecutionId",
                    column: x => x.ExecutionId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "CuttingExecutions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OffcutReports_ExecutionId",
            schema: "spaceos_cutting",
            table: "OffcutReports",
            column: "ExecutionId");

        // 6. MilestoneSubscriptions table
        migrationBuilder.CreateTable(
            name: "MilestoneSubscriptions",
            schema: "spaceos_cutting",
            columns: table => new
            {
                ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                MilestoneId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ConfigJson = table.Column<string>(type: "text", nullable: false),
                ConfigVersion = table.Column<int>(type: "integer", nullable: false),
                ReachedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MilestoneSubscriptions", x => new { x.ExecutionId, x.MilestoneId });
                table.ForeignKey(
                    name: "FK_MilestoneSubscriptions_CuttingExecutions_ExecutionId",
                    column: x => x.ExecutionId,
                    principalSchema: "spaceos_cutting",
                    principalTable: "CuttingExecutions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MilestoneSubscriptions_ExecutionId",
            schema: "spaceos_cutting",
            table: "MilestoneSubscriptions",
            column: "ExecutionId");

        // 7. OutboxMessages table (local transactional outbox)
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "spaceos_cutting",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                BatchSequenceNumber = table.Column<int>(type: "integer", nullable: true),
                AggregateId = table.Column<Guid>(type: "uuid", nullable: true),
                AggregateType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                EventType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                Status = table.Column<short>(type: "smallint", nullable: false),
                Attempts = table.Column<int>(type: "integer", nullable: false),
                LastError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        // Partial index — only pending messages need to be polled
        migrationBuilder.Sql(@"
CREATE INDEX ""IX_OutboxMessages_Status"" ON spaceos_cutting.""OutboxMessages"" (""Status"", ""OccurredAt"")
WHERE ""Status"" = 1;
");

        // 8. RLS on CuttingExecutions and child tables
        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.""CuttingExecutions"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""CuttingExecutions"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""CuttingExecutions""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);

ALTER TABLE spaceos_cutting.""OutboxMessages"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""OutboxMessages"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""OutboxMessages""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");

        // 9. Append-only trigger on ProgressEvents — prevents deletes/updates
        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION spaceos_cutting.prevent_progress_event_mutation()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'ProgressEvents is append-only — DELETE and UPDATE are not permitted';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_progress_events_append_only
    BEFORE DELETE OR UPDATE ON spaceos_cutting.""ProgressEvents""
    FOR EACH ROW EXECUTE FUNCTION spaceos_cutting.prevent_progress_event_mutation();
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_progress_events_append_only ON spaceos_cutting.""ProgressEvents"";
DROP FUNCTION IF EXISTS spaceos_cutting.prevent_progress_event_mutation();

DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""OutboxMessages"";
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.""CuttingExecutions"";
");

        migrationBuilder.DropTable(name: "MilestoneSubscriptions", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "OffcutReports", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "ProgressEvents", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "OutboxMessages", schema: "spaceos_cutting");
        migrationBuilder.DropTable(name: "CuttingExecutions", schema: "spaceos_cutting");

        // Restore Phase 3 stub table
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

        migrationBuilder.CreateIndex("IX_CuttingExecutions_TenantId", "CuttingExecutions", "TenantId", schema: "spaceos_cutting");
        migrationBuilder.CreateIndex("IX_CuttingExecutions_CuttingSheetId", "CuttingExecutions", "CuttingSheetId", schema: "spaceos_cutting", unique: true);
        migrationBuilder.CreateIndex("IX_CuttingExecutions_TenantId_CompletedAt", "CuttingExecutions", new[] { "TenantId", "CompletedAt" }, schema: "spaceos_cutting");

        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.""CuttingExecutions"" ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.""CuttingExecutions"" FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.""CuttingExecutions""
    USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");
    }
}
