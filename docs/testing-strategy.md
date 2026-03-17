# Testing Strategy

## Philosophy

Propentro follows the **Testing Trophy** approach (Kent C. Dodds): integration tests are the backbone, unit tests are reserved for risky logic.

**Why not 100% unit test coverage?**
- Most handlers are thin CQRS dispatchers — testing them in isolation duplicates integration test coverage without catching additional bugs.
- Integration tests (HTTP → MediatR → DB → response) catch the bugs that actually ship: routing, auth, serialization, database mapping, and pipeline behavior.
- Unit tests add value only where logic is complex enough to have edge cases that integration tests don't efficiently cover.

**Guiding principle:** Maximize bug-per-test ROI. Write the fewest tests that catch the most bugs.

## Test Layers

| Project | Type | What It Tests | When to Add |
|---|---|---|---|
| `Propentro.Api.Tests` | Integration | Full HTTP → DB → response cycle | Every endpoint (mandatory) |
| `Propentro.Application.Tests` | Unit | Validators, outbox event handlers | Validators with 2+ rules or conditional logic; event handlers with side effects |
| `Propentro.Domain.Tests` | Unit | Entity state transitions, value objects | State transition methods, complex domain logic |
| `Propentro.Infrastructure.Tests` | Unit | Email templates, external service logic | Template rendering, formatting, external service wrappers |
| `Propentro.Architecture.Tests` | Structural | Layer dependencies, CQRS conventions | New architectural rules (rare) |

## Integration Test Case Matrix

Every endpoint integration test must cover the applicable cases from this matrix:

| Test Case | Applies To | Expected Status |
|---|---|---|
| Happy path | All endpoints | 200/201/204 |
| Unauthenticated | All endpoints (except webhooks) | 401 |
| Missing permission | Endpoints requiring specific permissions | 403 |
| Validation failure | Endpoints with a FluentValidation validator | 400 |
| Resource not found | GET/PATCH/DELETE on single resources | 404 |
| Cross-tenant access | Org-scoped GET/PATCH/DELETE | 404 (not 403 — multi-tenancy filter hides it) |
| State conflict | State transition endpoints (deactivate/reactivate/cancel) | 409 |
| Soft-deleted filtering | Listing endpoints | 200 (inactive items excluded) |
| Hard delete confirmation | Hard delete endpoints | Requires `X-Confirm-Delete` header |

## Risky Area Definition

### Always Require Unit Tests
- **State transitions** — entity methods that change status (e.g., `Deactivate()`, `Reactivate()`, `Cancel()`)
- **Hard delete / cascade operations** — destructive actions with data loss risk
- **Permission/authorization logic** — custom authorization handlers
- **Money calculations** — minor unit arithmetic (rounding, conversion)
- **Conditional business logic** — if/else branching in handlers (e.g., existing user vs. new user invite flow)
- **External service integration** — email templates, webhook payload processing

### Not Risky (Integration Test Sufficient)
- **Simple validators** — single rule, no conditional logic (e.g., `NotEmpty`, `MaxLength`)
- **CRUD listings and simple GETs** — thin Dapper queries with no branching
- **Dapper read queries** — SQL correctness verified through integration tests
- **EF Core configurations** — column mappings verified through integration tests

## Decision Tree

**"I changed X — which test do I write?"**

```
Start
  │
  ├─ Is it a new endpoint?
  │   └─ YES → Integration test (see matrix above). MANDATORY.
  │         └─ Does the handler have conditional logic?
  │              └─ YES → Also unit test the handler/entity logic
  │
  ├─ Is it a state transition method on an entity?
  │   └─ YES → Unit test in Domain.Tests
  │
  ├─ Is it a validator?
  │   ├─ 2+ rules or conditional logic → Unit test in Application.Tests
  │   └─ Simple (NotEmpty, MaxLength only) → No unit test needed
  │
  ├─ Is it an outbox event handler?
  │   └─ YES → Unit test in Application.Tests
  │
  ├─ Is it infrastructure logic (templates, formatting)?
  │   └─ YES → Unit test in Infrastructure.Tests
  │
  └─ Is it a simple CRUD handler or Dapper query?
      └─ NO unit test needed — integration test covers it
```

## Conscious Exclusions

These areas are intentionally **not** unit tested, with rationale:

| Exclusion | Rationale |
|---|---|
| Simple validators (single `NotEmpty`/`MaxLength` rules) | Integration test 400 case verifies the pipeline catches validation errors; unit testing would only duplicate this |
| CRUD handlers with no branching | Handler is a thin dispatcher — EF Core/Dapper correctness proven by integration tests hitting a real DB |
| Dapper read queries | SQL correctness is only verifiable against a real database; mocking the connection proves nothing |
| EF Core entity configurations | Column mapping correctness is verified when integration tests read/write real data |
| Entity classes without state transition methods | No logic to test — they are data containers. If state transitions are added later, unit tests must be added then |

## Test Infrastructure

### Shared Factory (`PropentroApiFactory`)
- Uses Testcontainers with `PostgreSqlBuilder("postgres:17-alpine")`
- Requires Docker Desktop running
- `IClassFixture<PropentroApiFactory>` — all tests in a class share one server instance
- Never do destructive DB operations that would break other tests in the same class

### Database Seeder
Seeds a consistent baseline via raw SQL (bypasses EF Core filters):
- 2 organizations (`Organization1PublicId`, `Organization2PublicId`)
- 3 users (`User1PublicId` in org1, `User2PublicId` in org2, `AdminUserPublicId` as system admin)
- Roles and permissions for each organization

### HTTP Client Helpers
- `CreateClientAsUser1()` — org1, all permissions
- `CreateClientAsUser2()` — org2
- `CreateClientAsAdmin()` — system admin
- `CreateAnonymousClient()` — no auth token

### Conventions
- Fake IDs: `usr_000000000000`, `org_000000000000`, `rol_000000000000`, `inv_000000000000`
- Cross-tenant: User1 (org1) tries to access org2 resource → expect 404 (not 403)
- 409 state guards: deactivate already-inactive entity, reactivate already-active entity
