---
name: Cutting Phase 6 S1-S3 Status
description: Phase 6 S1-S3 Adapter Foundation + Framework + Resolver — CLOSED_DONE, 861 tests passing (2026-04-28)
type: project
---

Phase 6 S1-S3 adapter infrastructure CLOSED_DONE as of 2026-04-28.

**Why:** Adds pluggable adapter infrastructure for external cutting systems (OptiCut, CutRite, Manual).

**How to apply:** All subsequent work in the Cutting module can reference these adapter interfaces.

## What was implemented

### S1 — Domain + Application + Infrastructure + Migrations
- `Domain/Adapters/TenantCuttingProviderConfig.cs` — aggregate, optimistic versioning (int Version), allowed adapters: builtin/opticut/cutrite/manual, transports: none/file-exchange/rest-api/cli-wrapper
- `Domain/Adapters/AdapterHealthRecord.cs` — composite key (TenantId, AdapterName), sanitizes errors on RecordFailure
- `Domain/Adapters/AuditSanitizer.cs` — PUBLIC static class (needed by Infrastructure too), strips [\x00-\x1F\x7F], max 8000 chars
- `Domain/Adapters/Events/` — 5 domain event records
- `Application/Adapters/` — 6 interfaces: ITenantCuttingProviderConfigRepository, IAdapterHealthRecordRepository, IAdapterCallAuditWriter, IAdapterFactory, ICuttingProviderResolver, IConfigSecretDetector
- `Application/Adapters/ConfigSecretDetector.cs` — Shannon entropy >4.5 blocks, ${secret:name} refs allowed
- `Application/Adapters/CuttingProviderResolver.cs` — 30s distributed cache, Func<Guid> tenantIdResolver injection
- `Infrastructure/Adapters/` — 3 repositories + AdapterCallAuditWriter (SEC-08 sanitization)
- `Infrastructure/Persistence/Configurations/Adapters/` — 3 EF configurations (snake_case table names)
- `Infrastructure/Migrations/20260428000001..00005_*` — 5 manual migrations (no DB connection needed)
- CuttingDbContext updated with 3 new DbSet properties

### S2 — Transport layer
- `Infrastructure/Adapters/Transport/` — IExternalAdapterTransport, AdapterPayload, TransportSubmitResult, FileExchangeTransport, RestApiTransport, CliWrapperTransport, IpRangeChecker
- `Infrastructure/Adapters/FileSystem/` — ITenantAdapterStorage, TenantAdapterStorage (SEC-01 path validation, symlink rejection)
- `Infrastructure/Adapters/Resilience/BoundedSubprocessRunner.cs` — SEC-05 ArgumentList, SEC-18 1MB output cap
- `Infrastructure/Adapters/Format/IAdapterFormatConverter.cs`
- `Infrastructure/Adapters/AdapterFactory.cs`

### S3 — Background + Registration
- `Infrastructure/Adapters/Background/AdapterConfigInvalidationListener.cs` — BE-04 stub (Redis pub/sub todo)
- `Infrastructure/Adapters/Background/PollSchedulerBackgroundService.cs` — BE-04, BE-07 (Channel), BE-09 (TimeProvider), SEC-10 (SemaphoreSlim max 10)

## Package changes
- Application csproj: added Microsoft.Extensions.Caching.Abstractions 8.0.0, SpaceOS.Modules.Cutting.Contracts ProjectReference
- Test csproj: added Microsoft.Extensions.Caching.Memory 8.0.11 (then removed — use mock-based IDistributedCache in tests)

## Test counts
- Before: 734 (724 main + 10 contracts)
- After: 861 (851 main + 10 contracts)
- New tests: 127

## Key patterns established
- `CuttingProviderResolver` takes a `Func<Guid> tenantIdResolver` injection (not ICuttingTenantAccessor directly) for testability
- Tests use a mock-based IDistributedCache (dictionary-backed) to avoid NuGet version conflicts
- `BoundedSubprocessRunner` wraps stdout/stderr read in try/catch(OperationCanceledException) for timeout safety
- AuditSanitizer must be `public` (not internal) because Infrastructure uses it directly
- Migration files are hand-crafted (no Designer.cs files needed when using Sql() only migrations)
