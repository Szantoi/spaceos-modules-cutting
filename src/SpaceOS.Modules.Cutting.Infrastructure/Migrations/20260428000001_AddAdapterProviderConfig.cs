using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 6 S1: Creates tenant_cutting_provider_config table with RLS FORCE.
/// One row per tenant; versioned for optimistic concurrency.
/// </summary>
public partial class AddAdapterProviderConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
            constraints: table => table.PrimaryKey("PK_tenant_cutting_provider_config", x => x.tenant_id));

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

        migrationBuilder.Sql(@"
ALTER TABLE spaceos_cutting.tenant_cutting_provider_config ENABLE ROW LEVEL SECURITY;
ALTER TABLE spaceos_cutting.tenant_cutting_provider_config FORCE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON spaceos_cutting.tenant_cutting_provider_config
    USING (tenant_id = COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid);
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP POLICY IF EXISTS tenant_isolation ON spaceos_cutting.tenant_cutting_provider_config;
ALTER TABLE spaceos_cutting.tenant_cutting_provider_config DISABLE ROW LEVEL SECURITY;
");
        migrationBuilder.DropTable(name: "tenant_cutting_provider_config", schema: "spaceos_cutting");
    }
}
