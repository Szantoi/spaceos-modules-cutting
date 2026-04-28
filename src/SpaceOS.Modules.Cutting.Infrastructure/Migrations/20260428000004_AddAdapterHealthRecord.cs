using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 6 S1: Creates adapter_health_record table with composite PK (tenant_id, adapter_name) and RLS.
/// </summary>
public partial class AddAdapterHealthRecord : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.CreateIndex(
            name: "ix_ahr_tenant_id",
            schema: "spaceos_cutting",
            table: "adapter_health_record",
            column: "tenant_id");

        migrationBuilder.CreateIndex(
            name: "ix_ahr_is_healthy",
            schema: "spaceos_cutting",
            table: "adapter_health_record",
            column: "is_healthy");

        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.adapter_health_record ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.adapter_health_record FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.adapter_health_record
    USING (tenant_id = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.adapter_health_record;
ALTER TABLE spaceos_cutting.adapter_health_record DISABLE ROW LEVEL SECURITY;
");
        migrationBuilder.DropTable(name: "adapter_health_record", schema: "spaceos_cutting");
    }
}
