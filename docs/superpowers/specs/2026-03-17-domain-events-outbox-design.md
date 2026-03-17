# Domain Events & Outbox Handlers — Design Spec

**Date:** 2026-03-17
**Scope:** Identity & admin features — event raising + outbox email handlers
**Status:** Draft

## Context

The Propentro API has a fully built outbox infrastructure (domain events → `outbox_messages` table → Quartz job processing with retry/dead-letter). However, zero `IOutboxEventHandler` implementations exist, and existing Contact/Property events have no consumers. Identity entities (Organization, User, OrganizationUser) are not `AggregateRoot` and cannot use `RaiseDomainEvent`.

## Design Decisions

### Two event-raising mechanisms

1. **Entity lifecycle events** (existing) — `AggregateRoot.RaiseDomainEvent()` → collected in `SaveChangesAsync` → outbox. For business domain entities whose lifecycle events are intrinsic to the entity.
2. **Handler-level side-effect events** (new) — `IAppDbContext.AddOutboxEvent()` → handler creates event, added to DbSet, persisted in same transaction via `SaveChangesAsync`. For notification/email side-effects driven by handler actions on non-AggregateRoot entities.

**Why two mechanisms:** Identity entities (Organization, OrganizationUser) are structurally incompatible with `AggregateRoot` (Organization doesn't need `OrganizationId`; OrganizationUser is a join entity). Making them AggregateRoots just to raise events is wrong motivation. The handler-level mechanism complements the existing pattern without breaking entity hierarchy.

### Events carry only IDs, not data

Event payloads contain only `AggregateId` and `OrganizationId`. Handlers query the database for current data (email, name, org name) at processing time. This keeps events small and avoids stale data.

### Email via IEmailTemplateService

Email content is not embedded in handlers. `IEmailTemplateService` (interface in Application, implementation in Infrastructure) returns `EmailMessage` records. Handlers call the template service then `IEmailService.SendAsync`. This separates "what triggers email" from "what the email says."

### Retry strategy

Outbox handlers throw exceptions on failure. The existing Quartz job catches exceptions and applies exponential backoff (2^retryCount minutes, max 5 retries, then dead-letter). No changes to retry infrastructure needed.

## Cleanup

### Remove existing unused events and AggregateRoot demotion

- **Delete** all Contact events: `ContactCreatedEvent`, `ContactUpdatedEvent`, `ContactArchivedEvent`, `ContactUnarchivedEvent`, `ContactDeletedEvent`
- **Delete** all Property events: `PropertyCreatedEvent`, `PropertyUpdatedEvent`
- **Delete** unused Identity events: `MembershipCreatedEvent`, `MembershipRemovedEvent`
- **Delete** `MarkAs*` methods from `Contact` and `Property` entities
- **Remove** `MarkAs*` calls from command handlers that reference them
- **Demote** `Contact` and `Property` from `AggregateRoot` to `OrganizationEntity` — they no longer raise domain events. Can be promoted back when business domain events are needed.

## New Components

### 1. AddOutboxEvent on IAppDbContext

```csharp
// IAppDbContext
void AddOutboxEvent(IDomainEvent domainEvent, Guid? createdById = null);
```

Implementation in `AppDbContext`: creates `OutboxMessage` from the event and adds to `OutboxMessages` DbSet. Persisted in the same transaction as other changes when `SaveChangesAsync` is called by `UnitOfWorkBehavior`.

### 2. OutboxEventHandler\<TEvent\> base class

```csharp
public abstract class OutboxEventHandler<TEvent> : IOutboxEventHandler
    where TEvent : IDomainEvent
{
    public abstract string EventType { get; }

    public Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var @event = OutboxJsonOptions.Deserialize<TEvent>(payload);
        return HandleAsync(@event, cancellationToken);
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

Eliminates boilerplate deserialization in every handler. Concrete handlers override the typed `HandleAsync` method.

### 3. Domain Events (8 total)

All in `Domain/Entities/Identity/Events/`:

| Event Record | EventType String | Raised By |
|---|---|---|
| `MemberDeactivatedEvent` | `member.deactivated` | `DeactivateMemberHandler` (both admin and org-scoped) |
| `MemberReactivatedEvent` | `member.reactivated` | `ReactivateMemberHandler` (both admin and org-scoped) |
| `MemberRemovedEvent` | `member.removed` | `RemoveMemberHandler` |
| `MemberRolesUpdatedEvent` | `member.roles_updated` | `UpdateMemberRolesHandler` |
| `InviteAcceptedEvent` | `invite.accepted` | `SyncUserHandler` (webhook — invite accepted on membership create) |
| `InviteCanceledEvent` | `invite.canceled` | `CancelInviteHandler` |
| `OrganizationDeactivatedEvent` | `organization.deactivated` | `DeactivateOrganizationHandler` |
| `OrganizationReactivatedEvent` | `organization.reactivated` | `ReactivateOrganizationHandler` |

Each event follows existing pattern:
```csharp
public sealed record MemberDeactivatedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.deactivated";
}
```

### 4. Outbox Event Handlers (8 total)

Located in Application layer alongside their feature:

```
Application/
  Members/EventHandlers/
    MemberDeactivatedEventHandler.cs
    MemberReactivatedEventHandler.cs
    MemberRemovedEventHandler.cs
    MemberRolesUpdatedEventHandler.cs
  Invites/EventHandlers/
    InviteAcceptedEventHandler.cs
    InviteCanceledEventHandler.cs
  Organizations/EventHandlers/
    OrganizationDeactivatedEventHandler.cs
    OrganizationReactivatedEventHandler.cs
```

Each handler:
1. Deserializes event (via base class)
2. Queries DB with Dapper for recipient data (email, name, org name)
3. Calls `IEmailTemplateService` for email content
4. Calls `IEmailService.SendAsync`
5. Throws on failure → outbox retries

### 5. IEmailTemplateService

Interface in `Application/Common/Interfaces/Email/`. Implementation in `Infrastructure/Email/EmailTemplateService.cs`.

Methods return `EmailMessage` for each event type. Simple inline HTML templates initially — can migrate to React Email or template engine later.

### 6. Email Recipient Matrix

| Event | Recipient(s) | Query |
|---|---|---|
| `member.deactivated` | Deactivated member's email | User by membership AggregateId |
| `member.reactivated` | Reactivated member's email | User by membership AggregateId |
| `member.removed` | Removed member's email | User by membership AggregateId |
| `member.roles_updated` | Member whose roles changed | User by membership AggregateId + new role names |
| `invite.accepted` | Accepted user's email | User by AggregateId (invite) → invitee email |
| `invite.canceled` | Invitee's email | Invite by AggregateId → email field |
| `organization.deactivated` | All active members' emails | All users in org by OrganizationId |
| `organization.reactivated` | All members' emails | All users in org by OrganizationId |

## Files Changed Summary

### New files
- `Domain/Entities/Identity/Events/` — 8 event records
- `Application/Common/Messaging/OutboxEventHandler.cs` — generic base class
- `Application/Members/EventHandlers/` — 4 handlers
- `Application/Invites/EventHandlers/` — 2 handlers
- `Application/Organizations/EventHandlers/` — 2 handlers
- `Application/Common/Interfaces/Email/IEmailTemplateService.cs`
- `Infrastructure/Email/EmailTemplateService.cs`

### Modified files
- `Application/Common/Interfaces/Persistence/IAppDbContext.cs` — add `AddOutboxEvent` method
- `Infrastructure/Persistence/AppDbContext.cs` — implement `AddOutboxEvent`
- Command handlers (8 handlers) — add `dbContext.AddOutboxEvent(...)` calls
- `Domain/Entities/Contacts/Contact.cs` — remove `MarkAs*` methods, demote to `OrganizationEntity`
- `Domain/Entities/Properties/Property.cs` — remove `MarkAs*` methods, demote to `OrganizationEntity`
- Handlers referencing `MarkAs*` — remove those calls

### Deleted files
- `Domain/Entities/Contacts/Events/` — all 5 event files
- `Domain/Entities/Properties/Events/` — all 2 event files
- `Domain/Entities/Identity/Events/MembershipCreatedEvent.cs`
- `Domain/Entities/Identity/Events/MembershipRemovedEvent.cs`
