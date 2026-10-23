# propentro-api

The API behind Propentro, a multi-tenant property management SaaS I built alone in early 2026. **Paused.** Its lessons shaped my [Multi-tenant SaaS Starter](https://github.com/sufyant/multi-tenant-saas-starter).

## What it does

Organisations manage properties, units, contacts (tenants, owners, suppliers, staff) and compliance tracking. Users sign in with Clerk and act within one organisation, with permission-based roles.

## How it is built

- .NET 10, layered: Api, Application, Domain, Infrastructure, Contracts
- CQRS: commands write through EF Core, queries read through Dapper
- Domain events saved to an outbox in the same transaction, processed by a background job with retry
- Organisation scoping through EF Core global query filters
- Redis cache with invalidation after commit, S3 storage, Resend email
- Tests: domain, application, architecture rules (NetArchTest) and integration tests against real PostgreSQL (Testcontainers)

Schema: [propentro-db](https://github.com/propentro/propentro-db) · Frontend: [propentro-app](https://github.com/propentro/propentro-app)

## What the Starter does differently

- **Row Level Security instead of query filters.** A missing filter in code can no longer leak another tenant's data.
- **One repo, modular monolith** instead of separate API, app and database repos.
- **No commercially licensed packages.** MediatR is gone; the Starter uses Wolverine for handlers, outbox and sagas.
- **Invitations owned by the app** instead of Clerk webhooks.
