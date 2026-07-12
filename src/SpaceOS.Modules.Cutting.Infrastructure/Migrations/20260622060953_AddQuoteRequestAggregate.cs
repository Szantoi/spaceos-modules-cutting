using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteRequestAggregate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "WorkerId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "WindowStart",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "WindowEnd",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "adapter_call_audit",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    call_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adapter_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transport_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    method_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payload_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload_size_bytes = table.Column<int>(type: "integer", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    error_message = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adapter_call_audit", x => new { x.call_id, x.started_at });
                });

            migrationBuilder.CreateTable(
                name: "adapter_health_record",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adapter_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_check_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_success_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_healthy = table.Column<bool>(type: "boolean", nullable: false),
                    last_error_message = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adapter_health_record", x => new { x.tenant_id, x.adapter_name });
                });

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

            migrationBuilder.CreateTable(
                name: "quote_requests",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrackingToken = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    customer_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    items = table.Column<string>(type: "jsonb", nullable: false),
                    delivery_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requested_delivery_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    quoted_price_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    quoted_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConvertedToOrderAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CuttingSheetId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quote_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_cutting_provider_config",
                schema: "spaceos_cutting",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adapter_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    transport_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
                    config_schema_version = table.Column<short>(type: "smallint", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_cutting_provider_config", x => x.tenant_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_aca_tenant_id",
                schema: "spaceos_cutting",
                table: "adapter_call_audit",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_aca_tenant_started_at",
                schema: "spaceos_cutting",
                table: "adapter_call_audit",
                columns: new[] { "tenant_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_ahr_is_healthy",
                schema: "spaceos_cutting",
                table: "adapter_health_record",
                column: "is_healthy");

            migrationBuilder.CreateIndex(
                name: "ix_ahr_tenant_id",
                schema: "spaceos_cutting",
                table: "adapter_health_record",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_BatchAssignments_BatchId_PlanDate",
                schema: "spaceos_cutting",
                table: "BatchAssignments",
                columns: new[] { "BatchId", "PlanDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchAssignments_TenantId",
                schema: "spaceos_cutting",
                table: "BatchAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_CreatedAt",
                schema: "spaceos_cutting",
                table: "quote_requests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_CuttingSheetId",
                schema: "spaceos_cutting",
                table: "quote_requests",
                column: "CuttingSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_QuoteNumber",
                schema: "spaceos_cutting",
                table: "quote_requests",
                column: "QuoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_Status",
                schema: "spaceos_cutting",
                table: "quote_requests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_TenantId_Status",
                schema: "spaceos_cutting",
                table: "quote_requests",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_quote_requests_TrackingToken",
                schema: "spaceos_cutting",
                table: "quote_requests",
                column: "TrackingToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tcpc_adapter_name",
                schema: "spaceos_cutting",
                table: "tenant_cutting_provider_config",
                column: "adapter_name");

            migrationBuilder.CreateIndex(
                name: "ix_tcpc_is_enabled",
                schema: "spaceos_cutting",
                table: "tenant_cutting_provider_config",
                column: "is_enabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "adapter_call_audit",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "adapter_health_record",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "BatchAssignments",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "quote_requests",
                schema: "spaceos_cutting");

            migrationBuilder.DropTable(
                name: "tenant_cutting_provider_config",
                schema: "spaceos_cutting");

            migrationBuilder.DropColumn(
                name: "BatchId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions");

            migrationBuilder.DropColumn(
                name: "Priority",
                schema: "spaceos_cutting",
                table: "CuttingExecutions");

            migrationBuilder.AlterColumn<Guid>(
                name: "WorkerId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WindowStart",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "WindowEnd",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                schema: "spaceos_cutting",
                table: "CuttingExecutions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
