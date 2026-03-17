# Test Strategy & Coverage Design

**Date:** 2026-03-17
**Status:** Approved
**Approach:** Pragmatic (Testing Trophy)

## Overview

Define and implement a comprehensive test strategy for the Propentro API. All 39 endpoints must have integration tests. Unit tests are reserved for risky areas only. CLAUDE.md rules updated to enforce this going forward.

## Philosophy

The Testing Trophy model (Kent C. Dodds / Google testing guidance):

- **Integration tests are the backbone** — highest bug-per-test ROI, test the full stack (HTTP → DB → response)
- **Unit tests for risky logic only** — state transitions, deletions, authorization, money, conditional business logic
- **Architecture tests maintain structural integrity** — layer dependencies, CQRS conventions
- **Simple CRUD does not need unit tests** — integration tests provide sufficient coverage

## Current State

| Category | Total | Tested | Coverage |
|---|---|---|---|
| Endpoints (Integration) | 39 | 10 | 26% |
| Validators (Unit) | 21 | 2 | 9.5% |
| Outbox Event Handlers (Unit) | 9 | 3 | 33% |
| Domain Entities (Unit) | ~43 | 1 | ~2% |
| Infrastructure Services (Unit) | ~33 | 1 | ~3% |
| Architecture | 2 files | 17 tests | Sufficient |
| **Total tests** | — | **86** | — |

## Target State

| Category | Target | Notes |
|---|---|---|
| Endpoints (Integration) | 39/39 (100%) | All endpoints, no exceptions |
| Validators (Unit) | ~7 total | Only validators with 2+ rules or conditional logic |
| Outbox Event Handlers (Unit) | ~7 total | Only handlers with side effects |
| Domain Entities (Unit) | ~5 total | Only entities with state transition methods |
| Infrastructure Services (Unit) | 1 (existing) | EmailTemplateService already tested, others covered by integration |
| Architecture | No change | Current 17 tests sufficient |

## Integration Test Strategy

### Coverage Rules

Every endpoint gets at minimum:

| Test Case | When Required |
|---|---|
| Happy path (200/201/204) | Always |
| 401 Unauthorized | All endpoints (except webhook) |
| 403 Forbidden | All endpoints requiring specific permissions |
| 400 Validation | Endpoints with a validator |
| 404 Not Found | GET/PATCH/DELETE on single resources |
| 404 Cross-tenant | Org-scoped GET/PATCH/DELETE (User1 accessing Org2 resource) |
| 409 State guard | State transition endpoints (deactivate already inactive, etc.) |
| Soft delete filtering | Listing endpoints (deactivated entity must not appear) |
| X-Confirm-Delete header | Hard delete endpoints |
| Webhook signature validation | Webhook endpoints only |

### Missing Integration Tests (29 endpoints)

#### Org-Scoped Endpoints (10)

| Endpoint | Test File | Key Test Cases |
|---|---|---|
| `GET /members/{id}` | `GetMemberTests` | happy, 401, 403, 404, cross-tenant 404 |
| `PATCH /members/{id}/reactivate` | `ReactivateMemberTests` | happy, 401, 403, 404, 409 already active |
| `PATCH /members/{id}/roles` | `UpdateMemberRolesTests` | happy, 401, 403, 400, 404, cross-tenant |
| `GET /roles` | `GetRolesTests` | happy, 401, 403 |
| `GET /roles/{id}` | `GetRoleTests` | happy, 401, 403, 404, cross-tenant |
| `PATCH /roles/{roleId}` | `UpdateRoleTests` | happy, 401, 403, 400, 404 |
| `GET /permissions` | `GetPermissionsTests` | happy, 401, 403 |
| `PATCH /invites/{id}/cancel` | `CancelInviteTests` | happy, 401, 403, 404, 409 not pending |
| `GET /invites` | `GetInvitesTests` | happy, 401, 403 |
| `POST /webhooks/clerk` | `ClerkWebhookTests` | happy, invalid signature 401, idempotency |

#### Admin Endpoints (19)

| Endpoint | Test File | Key Test Cases |
|---|---|---|
| `GET /admin/organizations` | `GetOrganizationsTests` | happy, 401, 403 |
| `GET /admin/organizations/{id}` | `GetOrganizationTests` | happy, 401, 403, 404 |
| `PATCH /admin/organizations/{id}` | `UpdateOrganizationTests` | happy, 401, 403, 400, 404 |
| `PATCH /admin/organizations/{id}/reactivate` | `ReactivateOrganizationTests` | happy, 401, 403, 409 already active |
| `DELETE /admin/organizations/{id}` | `DeleteOrganizationTests` | happy, 401, 403, 404, X-Confirm-Delete |
| `GET /admin/users` | `GetUsersTests` | happy, 401, 403 |
| `GET /admin/users/{id}` | `GetUserTests` | happy, 401, 403, 404 |
| `PATCH /admin/users/{id}/deactivate` | `DeactivateUserTests` | happy, 401, 403, 404, 409 already inactive |
| `PATCH /admin/users/{id}/reactivate` | `ReactivateUserTests` | happy, 401, 403, 409 already active |
| `DELETE /admin/users/{id}` | `DeleteUserTests` | happy, 401, 403, 404, X-Confirm-Delete |
| `GET /admin/me` | `AdminGetProfileTests` | happy, 401, 403 |
| `PATCH /admin/organizations/{organizationId}/members/{id}/deactivate` | `AdminDeactivateMemberTests` | happy, 401, 403, 404, 409 |
| `PATCH /admin/organizations/{organizationId}/members/{id}/reactivate` | `AdminReactivateMemberTests` | happy, 401, 403, 404, 409 |
| `DELETE /admin/organizations/{organizationId}/members/{id}` | `AdminRemoveMemberTests` | happy, 401, 403, 404 |
| `GET /admin/organizations/{organizationId}/members` | `AdminGetMembersTests` | happy, 401, 403, 404 |
| `POST /admin/organizations/{organizationId}/invites` | `AdminInviteUserTests` | happy, 401, 403, 400, 200 (existing user) vs 201 (new user) |
| `GET /admin/organizations/{organizationId}/invites` | `AdminGetOrgInvitesTests` | happy, 401, 403, 404 |
| `PATCH /admin/invites/{id}/cancel` | `AdminCancelInviteTests` | happy, 401, 403, 409 not pending |
| `GET /admin/invites` | `AdminGetInvitesTests` | happy, 401, 403 |

## Unit Test Strategy

### Risky Area Definition

Unit tests are required when code contains:

| Risk Category | Example | Why |
|---|---|---|
| State transitions | `entity.Deactivate()`, `invite.Accept()` | Multiple valid/invalid state paths, edge cases not caught by single integration test |
| Hard delete / cascade | `organization.Delete()` with cascading member removal | Irreversible, must verify all cascades |
| Permission/authorization logic | Custom permission checks beyond middleware | Security-critical, must verify deny paths |
| Money calculations | Minor units conversion, pricing logic | Financial correctness, rounding errors |
| Conditional business logic | If existing user → direct add, else → send invite | Multiple code paths in single handler |
| External service integration | Email template rendering, webhook payload processing | Pure logic extractable from external dependency |

### Not Risky (No Unit Test Needed)

| Category | Why Integration Test Suffices |
|---|---|
| Simple validators (`Id.IsNotEmpty()`, `Name.MaxLength(100)`) | Integration test 400 case covers it |
| CRUD listings and simple GETs | No logic to unit test |
| Dapper read queries | Integration test hits real DB |
| EF Core configurations | Architecture test + integration test covers it |
| Simple command handlers (no conditional logic) | Integration test covers the full path |

### Entity Unit Tests (Domain.Tests)

| Entity | Methods to Test | Key Cases |
|---|---|---|
| `Organization` | `Deactivate()`, `Reactivate()` | active→inactive ✓, already inactive→error, inactive→active ✓, already active→error |
| `User` | `Deactivate()`, `Reactivate()` | Same state transition pattern |
| `OrganizationUser` | `Deactivate()`, `Reactivate()`, `Remove()` | State transitions + self-deactivation prevention |
| `Invite` | Already tested (8 tests) | Existing coverage sufficient |
| `Role` | Only if state logic exists | To be verified during implementation |

### Event Handler Unit Tests (Application.Tests)

| Handler | Status | What to Test |
|---|---|---|
| `OrganizationDeactivatedEventHandler` | Already tested | — |
| `InviteAcceptedEventHandler` | Already tested | — |
| `MemberDeactivatedEventHandler` | Already tested | — |
| `InviteCanceledEventHandler` | Needs test | Correct side effects triggered |
| `MemberReactivatedEventHandler` | Needs test | Correct side effects triggered |
| `MemberRemovedEventHandler` | Needs test | Cascade cleanup correct |
| `MemberRolesUpdatedEventHandler` | Needs test | Role change side effects |

### Validator Unit Tests (Application.Tests)

Only complex validators — those with 2+ rules or conditional logic:

| Validator | Why Unit Test |
|---|---|
| `InviteToCurrentOrgValidator` | Email format + multiple rules |
| `AdminInviteUserValidator` | Email + org validation |
| `UpdateMemberRolesValidator` | Role ID list validation, empty list check |
| `CreateOrganizationValidator` | Multiple fields (name, slug, etc.) |
| `UpdateOrganizationValidator` | Multiple fields |

Remaining 14 simple validators: no unit test, integration test 400 case sufficient.

## CLAUDE.md Changes

### Update "When to Write Which Test" Table

Replace existing table with:

| You added/changed... | Required test |
|---|---|
| New endpoint | Integration test: happy path, 401, 403, 400 (if validator), cross-tenant 404 (if org-scoped), 409 (if state transition) |
| State transition in entity | Unit test in Domain.Tests |
| Hard delete / cascade logic | Unit test + integration test with X-Confirm-Delete |
| Validator with 2+ rules or conditional logic | Unit test in Application.Tests |
| Validator with only simple rules | No unit test — integration test 400 case sufficient |
| Outbox event handler with side effects | Unit test in Application.Tests |
| Pure infrastructure logic (templates, formatting) | Unit test in Infrastructure.Tests |
| Simple CRUD handler | No unit test — integration test sufficient |

### Add "Risky Areas" Section

```markdown
### Risky Areas (Always Require Unit Tests)
- State transitions (entity methods that change status)
- Hard delete / cascade operations
- Permission/authorization logic
- Money calculations (minor units)
- Conditional business logic (if/else in handlers)
- External service integration logic (email templates, webhook processing)

### Not Risky (Integration Test Sufficient)
- Simple validators (single rule, no conditional logic)
- CRUD listings and simple GETs
- Dapper read queries
- EF Core configurations
```

### Add Reference to Strategy Document

Add to CLAUDE.md under Testing section:
```markdown
For the full testing philosophy, decision tree, and coverage matrix, see `docs/testing-strategy.md`.
```

### Note on Existing Integration Tests

The 10 existing integration test files should be audited in Phase 4 to ensure they cover the new required cases (cross-tenant 404, soft delete filtering, 409 state guards) where applicable. The existing `InviteUserTests` should also verify both 200 (existing user direct add) and 201 (new user invite) happy path branches.

## Test Strategy Document

Create `docs/testing-strategy.md` with:

1. Philosophy — Testing Trophy, pragmatic approach, why not 100% unit coverage
2. Test layers — purpose and scope of each layer
3. Risky area definition — with examples
4. Integration test case matrix — which endpoint type gets which test cases
5. Decision tree — "I changed X, which test do I write?"
6. Conscious exclusions — what we deliberately don't test and why

## Implementation Order

1. **Phase 1: CLAUDE.md + strategy doc** — Rules first, so all future work follows them
2. **Phase 2: Missing integration tests (29 endpoints)** — Coverage from 26% to 100%
3. **Phase 3: Risky area unit tests** — Entity, event handler, and complex validator tests
4. **Phase 4: Verify existing tests** — Ensure the 10 existing integration test files have all required cases (cross-tenant, soft delete, etc.)

## Estimated Test Count After Completion

| Category | Current | Added | Total |
|---|---|---|---|
| Integration tests | 25 | ~120-140 | ~145-165 |
| Validator unit tests | 17 | ~25-30 | ~42-47 |
| Event handler unit tests | 8 | ~8-12 | ~16-20 |
| Entity unit tests | 12 | ~12-16 | ~24-28 |
| Infrastructure unit tests | 7 | 0 | 7 |
| Architecture tests | 17 | 0 | 17 |
| **Total** | **86** | **~155-188** | **~241-274** |
