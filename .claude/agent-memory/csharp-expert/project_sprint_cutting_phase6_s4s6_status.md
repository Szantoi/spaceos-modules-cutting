---
name: Cutting Phase 6 S4-S6 status
description: Cutting Phase 6 S4-S6 Adapter Providers + Admin API + Hardening CLOSED_DONE — 931 tests (2026-04-28)
type: project
---

Cutting Phase 6 S4-S6 CLOSED_DONE — final phase 6 tasks.

**Why:** Complete the adapter framework with concrete provider implementations and admin management API.

**How to apply:** Phase 6 is fully complete. Next work should focus on integration or Phase 7 tasks.

## Deliverables

### S4: Adapter Implementations
- `src/.../Infrastructure/Adapters/Providers/BuiltinCuttingProvider.cs` — wraps CuttingProviderAdapter (BE-02 backward compat)
- `src/.../Infrastructure/Adapters/Providers/OptiCutAdapter.cs` + `OptiCutFormatConverter.cs` — FileExchangeTransport, SEC-02 XXE hardened
- `src/.../Infrastructure/Adapters/Providers/CutRiteAdapter.cs` + `CutRiteFormatConverter.cs` — CliWrapperTransport, CSV format
- `src/.../Infrastructure/Adapters/Providers/ManualAdapter.cs` — submit-only, no nesting
- `ServiceCollectionExtensions.cs` updated: all 4 adapters + IAdapterFactory + ICuttingProviderResolver + IDistributedCache

### S5: Admin API + CQRS
- `src/.../Application/Adapters/Commands/ConfigureAdapterCommand(Handler).cs`
- `src/.../Application/Adapters/Commands/TestAdapterCommand(Handler).cs`
- `src/.../Application/Adapters/Queries/GetAdapterConfigQuery(Handler).cs`
- `src/.../Application/Adapters/Queries/GetAdapterHealthQuery(Handler).cs`
- `src/.../Application/Adapters/Dtos/AdapterConfigDto.cs` (3 DTOs)
- `src/.../Api/Endpoints/AdapterAdminEndpoints.cs` — 4 endpoints, ManufacturerOnly
- `Program.cs` — `app.MapAdapterAdminEndpoints()` added

### S6: Hardening
- `operations/db_jobs/audit_retention.sql` — pg_cron partition cleanup function
- SEC-06 secret detector integrated into ConfigureAdapterCommandHandler

## Test counts
- Before: 851 main + 10 contracts = 861 total
- After: 921 main + 10 contracts = **931 total** (target was ≥ 931)
- New tests: 70
