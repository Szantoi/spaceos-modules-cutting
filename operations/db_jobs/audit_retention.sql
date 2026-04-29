-- ============================================================
-- Adapter Call Audit Log — Retention Cleanup (pg_cron job)
-- ============================================================
-- Purpose : Drop old monthly partitions of spaceos_cutting.adapter_call_audits
--           that exceed the configured retention window (default 90 days / 3 months).
--           Partition names follow the pattern: adapter_call_audits_YYYY_MM
--
-- Schedule : Run daily at 02:00 UTC via pg_cron:
--   SELECT cron.schedule(
--     'adapter-audit-retention',
--     '0 2 * * *',
--     $$SELECT spaceos_cutting.fn_drop_old_audit_partitions(90)$$
--   );
-- ============================================================

-- Create the cleanup function if it does not exist
CREATE OR REPLACE FUNCTION spaceos_cutting.fn_drop_old_audit_partitions(
    p_retention_days INTEGER DEFAULT 90
)
RETURNS INTEGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = spaceos_cutting, pg_temp
AS $$
DECLARE
    v_cutoff_date  DATE      := CURRENT_DATE - p_retention_days;
    v_partition    RECORD;
    v_dropped      INTEGER   := 0;
    v_partition_date DATE;
BEGIN
    FOR v_partition IN
        SELECT
            c.relname AS partition_name,
            n.nspname AS schema_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_inherits i   ON i.inhrelid = c.oid
        JOIN pg_class p      ON p.oid = i.inhparent
        WHERE n.nspname = 'spaceos_cutting'
          AND p.relname = 'adapter_call_audits'
          AND c.relkind = 'r'
        ORDER BY c.relname
    LOOP
        -- Extract YYYY_MM from name like adapter_call_audits_2026_01
        BEGIN
            v_partition_date := TO_DATE(
                SUBSTRING(v_partition.partition_name FROM '\d{4}_\d{2}$'),
                'YYYY_MM'
            );
        EXCEPTION WHEN OTHERS THEN
            -- Cannot parse date — skip this partition
            CONTINUE;
        END;

        -- The partition covers the entire month; it is safe to drop when the
        -- last day of that month is before the cutoff date.
        IF (DATE_TRUNC('month', v_partition_date) + INTERVAL '1 month - 1 day')::DATE < v_cutoff_date THEN
            EXECUTE FORMAT(
                'DROP TABLE IF EXISTS %I.%I',
                v_partition.schema_name,
                v_partition.partition_name
            );
            v_dropped := v_dropped + 1;

            RAISE NOTICE 'Dropped audit partition: %.%',
                v_partition.schema_name, v_partition.partition_name;
        END IF;
    END LOOP;

    RETURN v_dropped;
END;
$$;

-- Grant execute only to the pg_cron service role (not to spaceos_app)
REVOKE ALL ON FUNCTION spaceos_cutting.fn_drop_old_audit_partitions(INTEGER)
    FROM PUBLIC;
GRANT EXECUTE ON FUNCTION spaceos_cutting.fn_drop_old_audit_partitions(INTEGER)
    TO spaceos_admin;

COMMENT ON FUNCTION spaceos_cutting.fn_drop_old_audit_partitions(INTEGER) IS
    'Drops monthly partitions of adapter_call_audits older than p_retention_days days. '
    'Called daily by pg_cron at 02:00 UTC.';
