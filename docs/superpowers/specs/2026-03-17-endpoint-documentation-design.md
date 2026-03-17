# Endpoint Flow Documentation — Design Spec

**Date:** 2026-03-17
**Status:** Approved

## Goal

Create scannable, accurate documentation for every API endpoint covering validation rules, business logic flow, events raised, and response codes. Serves all consumers: backend developers, frontend developers, and test authors.

## File Structure

Documentation lives in `docs/endpoints/`, mirroring `Api/Endpoints/`:

```
docs/endpoints/
  ├── Organizations/
  │   ├── members.md
  │   ├── invites.md
  │   ├── roles.md
  │   └── settings.md
  ├── Admin/
  │   ├── organizations.md
  │   ├── members.md
  │   ├── invites.md
  │   ├── users.md
  │   └── me.md
  ├── me.md
  └── webhooks.md
```

Each file documents all endpoints defined in the corresponding endpoint file.

## Endpoint Entry Format

Each endpoint follows this format:

```markdown
## HTTP_METHOD /full/route/path

One-line description of what the endpoint does.

**Auth:** Required permission (e.g., `member:deactivate`) or `Authenticated` if no specific permission is needed.

**Validation:**
- `FieldName` — rule description
- `FieldName` — rule description

**Business Logic:**
1. Step description
2. Guard: condition not met → `Entity.ErrorCode`
3. Action performed
4. External service call (if any)

**Events:** `DomainEventName`

**Response:** 200 OK | 404 / 409 / 502
```

### Format Rules

- **Heading:** `## HTTP_METHOD /full/route/path`
- **Description:** Single sentence describing the action
- **Auth:** The permission string required, or `Authenticated` when no specific permission is checked
- **Validation:** FluentValidation rules, listed as `FieldName — rule`. Omit section if no validation (e.g., simple GET by ID)
- **Business Logic:** Numbered steps describing what happens. Guards use `Guard:` prefix with error code after `→`. Describe *what* is done, not *how* (no mention of EF/Dapper/specific infrastructure)
- **Events:** Domain event class name raised by the handler. Omit section entirely if no events are raised
- **Response:** Success status code, then pipe-separated possible error status codes
- **Ordering:** Endpoints appear in the same order as they are defined in the corresponding endpoint file

## Example

```markdown
## PATCH /v1/organizations/members/{id}/deactivate

Deactivates a member in the current organization.

**Auth:** `member:deactivate` permission

**Validation:**
- `Id` — required, not empty

**Business Logic:**
1. Resolve member by public ID within current organization
2. Guard: member not found → `Member.NotFound`
3. Guard: target is self → `Member.CannotDeactivateSelf`
4. Guard: already inactive → `Member.AlreadyDeactivated`
5. Set `IsActive = false`, update audit fields
6. Sync to Clerk: remove org membership

**Events:** `MemberDeactivatedDomainEvent`

**Response:** 204 No Content | 404 / 409 / 502
```

## CLAUDE.md Rule

Add to `Key Conventions` section:

> **Endpoint documentation**: Every endpoint must have a corresponding entry in `docs/endpoints/` following the structure mirroring `Api/Endpoints/`. When adding a new endpoint, add its documentation. When modifying a handler's business logic, validation, auth, or events, update the corresponding documentation. Format must match existing entries.

## What This Documentation Does NOT Cover

- Request/response body shapes — Scalar/OpenAPI handles this
- Infrastructure details (EF Core, Dapper, caching) — implementation concern
- Code examples — the source code is the example

## Maintenance

- New endpoint → add entry to the corresponding `docs/endpoints/` file
- Handler business logic change → update the corresponding entry
- Endpoint removed → remove the entry
- CLAUDE.md rule ensures this is enforced in every session
