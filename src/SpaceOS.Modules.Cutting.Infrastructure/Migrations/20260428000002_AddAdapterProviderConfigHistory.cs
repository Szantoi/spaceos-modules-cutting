using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 6 S1: Creates tenant_cutting_provider_config_history table with an append-only trigger.
/// History rows are inserted automatically by a trigger on the main config table.
/// </summary>
public partial class AddAdapterProviderConfigHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS spaceos_cutting.tenant_cutting_provider_config_history (
    history_id      uuid        NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       uuid        NOT NULL,
    adapter_name    varchar(50) NOT NULL,
    transport_name  varchar(50) NOT NULL,
    is_enabled      boolean     NOT NULL,
    config_json     jsonb       NOT NULL,
    config_schema_version smallint NOT NULL,
    version         int         NOT NULL,
    actor_id        uuid,
    change_reason   varchar(500),
    changed_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT PK_tcpc_history PRIMARY KEY (history_id)
);

CREATE INDEX ix_tcpch_tenant_version
    ON spaceos_cutting.tenant_cutting_provider_config_history (tenant_id, version);

-- Append-only: prevent delete/update on history
CREATE OR REPLACE FUNCTION spaceos_cutting.prevent_tcpc_history_mutation()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'tenant_cutting_provider_config_history is append-only';
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_tcpc_history_append_only
    BEFORE DELETE OR UPDATE ON spaceos_cutting.tenant_cutting_provider_config_history
    FOR EACH ROW EXECUTE FUNCTION spaceos_cutting.prevent_tcpc_history_mutation();
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_tcpc_history_append_only
    ON spaceos_cutting.tenant_cutting_provider_config_history;
DROP FUNCTION IF EXISTS spaceos_cutting.prevent_tcpc_history_mutation();
DROP TABLE IF EXISTS spaceos_cutting.tenant_cutting_provider_config_history;
");
    }
}
