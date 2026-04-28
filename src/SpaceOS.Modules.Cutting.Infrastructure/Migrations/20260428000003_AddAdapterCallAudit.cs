using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 6 S1: Creates adapter_call_audit as a range-partitioned table keyed on started_at.
/// Creates initial partitions for current and next month.
/// </summary>
public partial class AddAdapterCallAudit : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // EF Core maps this table, but the DDL uses PARTITION BY for native PostgreSQL partitioning.
        // EF itself does not write the PARTITION BY clause — we do it via raw SQL here.
        migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS spaceos_cutting.adapter_call_audit (
    call_id          uuid        NOT NULL,
    tenant_id        uuid        NOT NULL,
    adapter_name     varchar(50) NOT NULL,
    transport_name   varchar(50) NOT NULL,
    method_name      varchar(100) NOT NULL,
    correlation_id   varchar(200),
    payload_hash     varchar(128),
    payload_size_bytes int,
    started_at       timestamptz NOT NULL,
    completed_at     timestamptz,
    duration_ms      int,
    status           varchar(20) NOT NULL DEFAULT 'started',
    error_message    varchar(8000),
    user_id          uuid,
    PRIMARY KEY (call_id, started_at)
) PARTITION BY RANGE (started_at);

CREATE INDEX ix_aca_tenant_id
    ON spaceos_cutting.adapter_call_audit (tenant_id);

CREATE INDEX ix_aca_tenant_started_at
    ON spaceos_cutting.adapter_call_audit (tenant_id, started_at);

-- Create initial monthly partitions
DO $$
DECLARE
    v_start date := date_trunc('month', now());
    v_end   date;
    v_name  text;
BEGIN
    FOR i IN 0..1 LOOP
        v_start := date_trunc('month', now() + (i || ' months')::interval);
        v_end   := v_start + interval '1 month';
        v_name  := 'adapter_call_audit_' || to_char(v_start, 'YYYY_MM');
        EXECUTE format(
            'CREATE TABLE IF NOT EXISTS spaceos_cutting.%I
             PARTITION OF spaceos_cutting.adapter_call_audit
             FOR VALUES FROM (%L) TO (%L)',
            v_name, v_start, v_end);
    END LOOP;
END;
$$;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
DROP TABLE IF EXISTS spaceos_cutting.adapter_call_audit CASCADE;
");
    }
}
