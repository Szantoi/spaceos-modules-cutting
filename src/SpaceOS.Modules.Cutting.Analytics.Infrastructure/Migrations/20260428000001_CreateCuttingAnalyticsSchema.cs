using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Analytics.Infrastructure.Migrations;

/// <inheritdoc />
public partial class CreateCuttingAnalyticsSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "cutting_analytics");

        // ── DailyExecutionMetrics ─────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "DailyExecutionMetrics",
            schema: "cutting_analytics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MachineId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                MetricDate = table.Column<DateOnly>(type: "date", nullable: false),
                CompletedCount = table.Column<int>(type: "integer", nullable: false),
                AvgDurationMinutes = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                YieldPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DailyExecutionMetrics", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_DailyExecutionMetrics_TenantId",
            schema: "cutting_analytics",
            table: "DailyExecutionMetrics",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyExecutionMetrics_TenantId_MetricDate_MachineId",
            schema: "cutting_analytics",
            table: "DailyExecutionMetrics",
            columns: new[] { "TenantId", "MetricDate", "MachineId" },
            unique: true);

        migrationBuilder.Sql(@"
            ALTER TABLE cutting_analytics.""DailyExecutionMetrics"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE cutting_analytics.""DailyExecutionMetrics"" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON cutting_analytics.""DailyExecutionMetrics""
                USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
        ");

        // ── DailyMaterialUsages ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "DailyMaterialUsages",
            schema: "cutting_analytics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MaterialCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                TotalAreaUsedMm2 = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                WasteAreaMm2 = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                OffcutCount = table.Column<int>(type: "integer", nullable: false),
                LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DailyMaterialUsages", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_DailyMaterialUsages_TenantId",
            schema: "cutting_analytics",
            table: "DailyMaterialUsages",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyMaterialUsages_TenantId_UsageDate_MaterialCode",
            schema: "cutting_analytics",
            table: "DailyMaterialUsages",
            columns: new[] { "TenantId", "UsageDate", "MaterialCode" },
            unique: true);

        migrationBuilder.Sql(@"
            ALTER TABLE cutting_analytics.""DailyMaterialUsages"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE cutting_analytics.""DailyMaterialUsages"" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON cutting_analytics.""DailyMaterialUsages""
                USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
        ");

        // ── MachineOEEHourlies ────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "MachineOEEHourlies",
            schema: "cutting_analytics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                MachineId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                HourSlot = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Availability = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                Performance = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                Quality = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_MachineOEEHourlies", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_MachineOEEHourlies_TenantId",
            schema: "cutting_analytics",
            table: "MachineOEEHourlies",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_MachineOEEHourlies_TenantId_MachineId_HourSlot",
            schema: "cutting_analytics",
            table: "MachineOEEHourlies",
            columns: new[] { "TenantId", "MachineId", "HourSlot" },
            unique: true);

        migrationBuilder.Sql(@"
            ALTER TABLE cutting_analytics.""MachineOEEHourlies"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE cutting_analytics.""MachineOEEHourlies"" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON cutting_analytics.""MachineOEEHourlies""
                USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
        ");

        // ── DailyOperatorMetrics ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "DailyOperatorMetrics",
            schema: "cutting_analytics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                WorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                MetricDate = table.Column<DateOnly>(type: "date", nullable: false),
                CompletedExecutions = table.Column<int>(type: "integer", nullable: false),
                AvgDurationMinutes = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                IsSuppressed = table.Column<bool>(type: "boolean", nullable: false),
                LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_DailyOperatorMetrics", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_DailyOperatorMetrics_TenantId",
            schema: "cutting_analytics",
            table: "DailyOperatorMetrics",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_DailyOperatorMetrics_TenantId_MetricDate",
            schema: "cutting_analytics",
            table: "DailyOperatorMetrics",
            columns: new[] { "TenantId", "MetricDate" });

        migrationBuilder.Sql(@"
            ALTER TABLE cutting_analytics.""DailyOperatorMetrics"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE cutting_analytics.""DailyOperatorMetrics"" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON cutting_analytics.""DailyOperatorMetrics""
                USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
        ");

        // ── AnalyticsRebuildJobs ──────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "AnalyticsRebuildJobs",
            schema: "cutting_analytics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ErrorMessage = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                ProcessedChunks = table.Column<int>(type: "integer", nullable: false),
                TotalChunks = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AnalyticsRebuildJobs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AnalyticsRebuildJobs_TenantId_Status",
            schema: "cutting_analytics",
            table: "AnalyticsRebuildJobs",
            columns: new[] { "TenantId", "Status" });

        migrationBuilder.Sql(@"
            ALTER TABLE cutting_analytics.""AnalyticsRebuildJobs"" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE cutting_analytics.""AnalyticsRebuildJobs"" FORCE ROW LEVEL SECURITY;
            CREATE POLICY tenant_isolation ON cutting_analytics.""AnalyticsRebuildJobs""
                USING (""TenantId"" = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
        ");

        // ── ProcessedOutboxEvents ─────────────────────────────────────────────
        // No RLS — cross-tenant dedup ledger keyed on (EventId, SubscriberName) only.
        migrationBuilder.CreateTable(
            name: "ProcessedOutboxEvents",
            schema: "cutting_analytics",
            columns: table => new
            {
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                SubscriberName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProcessedOutboxEvents", x => new { x.EventId, x.SubscriberName }));

        migrationBuilder.CreateIndex(
            name: "IX_ProcessedOutboxEvents_CreatedAt",
            schema: "cutting_analytics",
            table: "ProcessedOutboxEvents",
            column: "CreatedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProcessedOutboxEvents", schema: "cutting_analytics");
        migrationBuilder.DropTable(name: "AnalyticsRebuildJobs", schema: "cutting_analytics");
        migrationBuilder.DropTable(name: "DailyOperatorMetrics", schema: "cutting_analytics");
        migrationBuilder.DropTable(name: "MachineOEEHourlies", schema: "cutting_analytics");
        migrationBuilder.DropTable(name: "DailyMaterialUsages", schema: "cutting_analytics");
        migrationBuilder.DropTable(name: "DailyExecutionMetrics", schema: "cutting_analytics");
    }
}
