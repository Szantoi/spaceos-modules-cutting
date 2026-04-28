using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpaceOS.Modules.Cutting.Infrastructure.Migrations;

/// <summary>
/// Phase 6 S1: Creates a <c>spaceos_cutting.uuidv7()</c> helper function that generates
/// time-ordered UUIDs v7 in PostgreSQL. Used for adapter_call_audit.call_id generation.
/// </summary>
public partial class AddUuidV7Function : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION spaceos_cutting.uuidv7()
RETURNS uuid AS $$
DECLARE
    v_time  double precision;
    v_secs  bigint;
    v_msec  bigint;
    v_unix  bigint;
    v_hi    bigint;
    v_lo    bigint;
    v_hex   text;
BEGIN
    v_time := extract(epoch FROM clock_timestamp());
    v_secs := floor(v_time);
    v_msec := floor((v_time - v_secs) * 1000);
    v_unix := (v_secs * 1000 + v_msec);

    -- 48-bit timestamp + version 7 nibble + 12 random bits
    v_hi := (v_unix << 16) | (7 << 12) | (floor(random() * 4096)::bigint);
    -- variant bits 10xx + 62 random bits
    v_lo := (2 << 62) | (floor(random() * (1::bigint << 62))::bigint);

    v_hex := lpad(to_hex(v_hi), 16, '0') || lpad(to_hex(v_lo), 16, '0');
    RETURN (
        substr(v_hex, 1, 8) || '-' ||
        substr(v_hex, 9, 4) || '-' ||
        substr(v_hex, 13, 4) || '-' ||
        substr(v_hex, 17, 4) || '-' ||
        substr(v_hex, 21, 12)
    )::uuid;
END;
$$ LANGUAGE plpgsql;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS spaceos_cutting.uuidv7();");
    }
}
