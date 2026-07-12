# SpaceOS.Modules.Cutting — CLAUDE.md

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "cutting",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/cutting/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/cutting/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"cutting","status":"idle"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Cutting státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Cutting swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## JELENLEGI ÁLLAPOT (2026-04-17)

| | |
|---|---|
| **Terminál** | cutting · Port: **5005** · Mailbox: `/opt/spaceos/docs/mailbox/cutting/` |
| **Aktuális commit** | `64fcf55` (CUTTING-015: OpenConnectionAsync affinity fix) |
| **Tesztek** | **77/77 pass** |
| **VPS** | LIVE ✅ |

### TenantGucKey
```
TenantGucKey = "app.current_tenant_id"
```

### InternalEndpoints.cs — OpenConnectionAsync minta (KÖTELEZŐ)
```csharp
if (dbContext.Database.IsRelational())
    await dbContext.Database.OpenConnectionAsync(ct);
try {
    if (dbContext.Database.IsRelational())
        await dbContext.Database.ExecuteSqlAsync(
            $"SELECT set_config('{TenantGucKey}', {tenantIdStr}, false)", ct);
    counts = await repo.DeleteByTenantAsync(tenantGuid, ct);
} finally {
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync();
}
```

---

## Stack
- .NET 8, Clean Architecture + DDD + CQRS
- PostgreSQL 16 sémák: `spaceos_inventory` · `spaceos_cutting` · `spaceos_procurement`
- EF Core 8 + Npgsql 8.0.11
- Port: **5005**

## Approved packages
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0 · Ardalis.Specification 8.0.0
EF Core 8.0.11 · Npgsql 8.0.11 · xUnit v3 · Moq 4.20.72 · FluentAssertions 6.12.2

A listán kívüli package hozzáadása explicit egyeztetést igényel.

## Solution structure (teljes kép)

```
src/
  SpaceOS.Modules.Inventory.Contracts    ← IInventoryProvider + DTOs (KÉSZ)
  SpaceOS.Modules.Cutting.Contracts      ← ICuttingProvider + DTOs (KÉSZ)
  SpaceOS.Modules.Procurement.Contracts  ← IProcurementProvider + DTOs (KÉSZ)

  SpaceOS.Modules.Inventory.Domain       ← aggregates, VOs, domain events
  SpaceOS.Modules.Inventory.Application  ← CQRS handlers, validators
  SpaceOS.Modules.Inventory.Infrastructure ← EF Core + PostgreSQL
  SpaceOS.Modules.Inventory.Api          ← Minimal API endpoints

  SpaceOS.Modules.Cutting.Domain         ← aggregates, VOs, domain events
  SpaceOS.Modules.Cutting.Application    ← CQRS handlers, validators
  SpaceOS.Modules.Cutting.Infrastructure ← EF Core + PostgreSQL
  SpaceOS.Modules.Cutting.Api            ← Minimal API endpoints

  SpaceOS.Modules.Procurement.Domain         ← aggregates, VOs, domain events
  SpaceOS.Modules.Procurement.Application    ← CQRS handlers, validators
  SpaceOS.Modules.Procurement.Infrastructure ← EF Core + PostgreSQL
  SpaceOS.Modules.Procurement.Api            ← Minimal API endpoints

tests/
  SpaceOS.Modules.Cutting.Contracts.Tests  ← Contracts smoke tests (KÉSZ, 9/9)
```

## Layer dependency rule (hard constraint)

```
Domain ← Application ← Infrastructure ← Api
                                       ← Tests
```

Contracts projektek Domain-tól is independensek — nincs ProjectReference a domain projectekre.
Domain-nak nincs külső NuGet dependency (csak Ardalis.Result). Bármilyen sértés → azonnal jelzés.

## Modul dependency sorrend

```
Inventory.Contracts (önálló)
    ↓
Cutting.Contracts    → Inventory.Contracts
Procurement.Contracts → Inventory.Contracts
    ↓
Inventory.Domain / Application / Infrastructure / Api  ← PHASE 1
    ↓
Cutting.Domain / Application / Infrastructure / Api    ← PHASE 2
    ↓
Procurement.Domain / Application / Infrastructure / Api ← PHASE 3
```

**Mindig Inventory-t implementálj először.** Cutting és Procurement az Inventory API-tól függ.

## Domain fogalomtár (rövid)

### Inventory
- **MaterialCatalog** — Anyagtörzs (MDF 18mm, HDF 3mm, ABS él, stb.). Ritkán változó referencia adat.
- **PanelStock** — Raktárkészlet: teljes táblák + maradékok (Offcut). Tenant-specifikus.
- **Offcut** — Maradék darab. Mérete + anyaga + eredete (melyik vágásból) ismert. Három sors: visszakerül készletbe / hulladék / felhasznált.
- **StockMovement** — Minden készletváltozás audit trail-je: bevételezés / felhasználás / maradék / selejt.
- **ConsumptionTrend** — Anyagfogyási trend, szezonalitás.

### Cutting
- **CuttingSheet** — Egy rendeléshez tartozó szabászati adatcsomag. **Immutable snapshot** — újraszámolásnál új keletkezik, régi megmarad (audit trail).
- **CuttingLine** — Egy alkatrész a listában: név, nyers méret, anyag, darabszám.
- **DailyCuttingPlan** — Napi szabászterv. Csoportosít anyagtípus szerint (kevesebb gépátállás).
- **CuttingExecution** — Végrehajtás tracking. FSM: `Planned → InProgress → Completed / Failed`.
- **Waste** — Hulladék. Cutting méri, Inventory-nak jelenti.

### Procurement
- **Supplier** — Szállító cég. Lead time, megbízhatósági rating.
- **PurchaseOrder** — Rendelés. FSM: `Draft → Submitted → Confirmed → Shipped → Delivered / Cancelled`.
- **Delivery** — Szállítás fogadás → bevételezés az Inventory-ba.
- **ReorderAlert** — Inventory küldi, ha készlet a threshold alá csökken.

## Naming conventions
| Scope | Convention |
|---|---|
| Classes, methods, properties | PascalCase |
| Private fields | _camelCase |
| Local variables | camelCase |
| CancellationToken param | mindig `ct` |
| File name | 1:1 with class name |

## Universal code rules
```csharp
// 1. ConfigureAwait(false) minden production async callban
await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);

// 2. CancellationToken neve mindig ct
public async Task<Result<T>> Handle(TRequest request, CancellationToken ct)

// 3. AsNoTracking() minden read-only lekérdezésnél
_db.PanelStocks.AsNoTracking().Where(...)

// 4. Result<T> minden handler return type
public async Task<Result<StockLevelResponse>> Handle(...)
```

## Golden Rules
1. Nincs public setter az aggregátokon
2. Business logic csak Domain-ben
3. Minden mutáció domain eventet ráz
4. PopDomainEvents() + dispatch minden mutating handler végén
5. Minden lista lekérdezés Ardalis.Specification-ön át
6. Result<T> minden handler return type-ja
7. ConfigureAwait(false) minden production async callban
8. AsNoTracking() minden read-only method-ban

## Kritikus szabályok
- **CuttingSheet immutable** — nincs UPDATE, ha újraszámolnak, új CuttingSheet keletkezik
- **StockMovement append-only** — nincs UPDATE, nincs DELETE — kizárólag INSERT
- **Offcut eredete nyomon követhető** — `OriginCuttingSheetId` mindig kitöltve
- **Minden endpoint: `[Authorize(Policy = "ManufacturerOnly")]`** (SEC-04)
- **RLS FORCE** minden tenant-specifikus táblán (PanelStock, Offcut, StockMovement, CuttingSheet, PurchaseOrder)
- **MaterialCatalog**: tenant-független config — `REVOKE INSERT/UPDATE/DELETE FROM spaceos_app` (csak SELECT app-felhasználónak)

## DB sémák

### spaceos_inventory
- `MaterialCatalog` — tenant-független, RLS nélkül (csak SELECT az app usereknek)
- `PanelStocks` — RLS FORCE (TenantId)
- `Offcuts` — RLS FORCE (TenantId)
- `StockMovements` — RLS FORCE (TenantId), append-only

### spaceos_cutting
- `CuttingSheets` — RLS FORCE (TenantId), immutable
- `CuttingLines` — RLS FORCE (TenantId)
- `DailyCuttingPlans` — RLS FORCE (TenantId)
- `CuttingExecutions` — RLS FORCE (TenantId)

### spaceos_procurement
- `Suppliers` — RLS FORCE (TenantId)
- `PurchaseOrders` — RLS FORCE (TenantId)
- `Deliveries` — RLS FORCE (TenantId)

## API surface

### Inventory (port 5004, prefix `/api/inventory`)
```
GET    /api/inventory/stock                   GetStock (anyagtípus szerint)
GET    /api/inventory/offcuts                 GetOffcuts (felhasználható maradékok)
POST   /api/inventory/movements/consumption   RecordConsumption
POST   /api/inventory/movements/inbound       RecordInbound
POST   /api/inventory/movements/offcut        RecordOffcut
GET    /api/inventory/trend                   GetConsumptionTrend
```

### Cutting (port 5004, prefix `/api/cutting`)
```
POST   /api/cutting/sheets                    SubmitCuttingSheet
GET    /api/cutting/sheets/{id}/nesting       GetNestingResult
GET    /api/cutting/sheets/{id}/status        GetExecutionStatus
GET    /api/cutting/waste                     GetWasteReport
POST   /api/cutting/plans                     CreateDailyCuttingPlan
GET    /api/cutting/plans/{date}              GetDailyCuttingPlan
```

### Procurement (port 5004, prefix `/api/procurement`)
```
POST   /api/procurement/orders                CreatePurchaseOrder
GET    /api/procurement/orders/{id}           GetOrderStatus
GET    /api/procurement/prices                GetSupplierPrices
POST   /api/procurement/deliveries            RecordDelivery
```

## DoD per fázis

- **Inventory Core (CUTTING-002):** ≥40 teszt (domain 15 · EF 10 · API 10 · security 5)
- **Cutting Core (CUTTING-003):** ≥35 teszt (domain 15 · EF 10 · API 10)
- **Procurement Core (CUTTING-004):** ≥30 teszt (domain 10 · EF 10 · API 10)

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS. Lásd teljes leírás: `/opt/spaceos/docs/WORKFLOW.md`

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
- `ls /opt/spaceos/docs/mailbox/cutting/inbox/` → legfrissebb UNREAD üzenet elolvasása
- Frontmatter: `status: UNREAD` → `status: READ`

### 2. CODE
- Implementálj a feladat szerint

### 3. BUILD
- `dotnet build` → **0 error, 0 warning** — ha nem: javítsd, ne lépj tovább

### 4. TEST
- `dotnet test` → **minden teszt zöld** — ha nem: javítsd, ne lépj tovább
- Új kódhoz új tesztet írj

### 5. REVIEW (önellenőrzés)
- Layer dependency rule betartva?
- Nincs public setter az aggregate-eken?
- Business logic csak Domain-ben?
- `ConfigureAwait(false)` minden async callban?
- `AsNoTracking()` minden read-only lekérdezésben?
- Nincs `TODO`/`FIXME` a kódban?

### 6. SECURITY ⚠️
- **Authorization**: minden endpoint `[Authorize(Policy = "ManufacturerOnly")]`?
- **RLS**: minden tenant-specifikus tábla RLS FORCE-al védve?
- **Immutability**: CuttingSheet és StockMovement nem módosítható?
- **MaterialCatalog**: REVOKE INSERT/UPDATE/DELETE `spaceos_app`-tól?
- **OWASP Top 10**: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Minden befejezett feladat után kötelező outbox üzenetet írni.
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `/opt/spaceos/docs/mailbox/cutting/outbox/`

```markdown
## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/cutting.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---
id: MSG-CUTTING-NNN-DONE
from: cutting
to: conductor
type: done
status: UNREAD
---

## Összefoglaló
[Mit implementáltál, mely fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted (RLS, auth policy, immutability, stb.)]

## Kockázatok / kérdések
[Ha van → status: BLOCKED és leírás]
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/cutting.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## Közös erőforrások

- **Inbox**: `/opt/spaceos/docs/mailbox/cutting/inbox/`
- **Outbox**: `/opt/spaceos/docs/mailbox/cutting/outbox/`
- **Codebase_Status.md**: `/opt/spaceos/docs/Codebase_Status.md`
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md`
- **Domain vision**: `/opt/spaceos/docs/tasks/new/SpaceOS_Modules_Cutting_Vision_v1.md`
- **Projekt vízió (üzleti)**: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md`
- **Technikai master overview**: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md`
- **`/spaceos-terminal` skill**: `/opt/spaceos/.claude/skills/spaceos-terminal/`

---

## BACKEND IMPLEMENTÁCIÓS CHECKLIST

Minden feature/bugfix végén, DONE outbox előtt:

- [ ] Entity creation factory method-dal (nem publikus constructor)
- [ ] Setter-ek private-ok
- [ ] Domain validation implementálva (nem controller-ben)
- [ ] Controller/endpoint csak DTO-t ad vissza (entity soha)
- [ ] Unit test üzleti logikára
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` minden zöld

### QA Handoff kritérium

A TESTER terminált ROOT hívja be ha a feladat:
- Üzleti validációs logikát tartalmaz (pl. rendelés állapotgép, ár kalkuláció)
- Pénzügyi számítást végez
- Workflow / FSM state machine-t érint
- A task explicit jelzi: "QA needed: Yes"

Egyszerű CRUD endpoint-ok NEM igényelnek QA-t, kivéve ha explicit kérve van.
