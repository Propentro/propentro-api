# Domain Events & Outbox Handlers — Design Spec

**Date:** 2026-03-17
**Scope:** Identity & admin features — event raising + outbox email handlers
**Status:** Draft

## Context

The Propentro API has a fully built outbox infrastructure (domain events → `outbox_messages` table → Quartz job processing with retry/dead-letter). However, zero `IOutboxEventHandler` implementations exist, and existing Contact/Property events have no consumers. Identity entities (Organization, User, OrganizationUser) are not `AggregateRoot` and cannot use `RaiseDomainEvent`.

`AddOutboxEvent(IDomainEvent)` already exists on `IAppDbContext` and `AppDbContext` — it adds events to a `_pendingEvents` list that gets persisted in `PersistDomainEventsToOutbox` during `SaveChangesAsync`, alongside AggregateRoot-collected events.

## Design Decisions

### Two event-raising mechanisms

1. **Entity lifecycle events** (existing) — `AggregateRoot.RaiseDomainEvent()` → collected in `SaveChangesAsync` → outbox. For business domain entities whose lifecycle events are intrinsic to the entity.
2. **Handler-level side-effect events** (existing method, first real usage) — `IAppDbContext.AddOutboxEvent()` → handler creates event, added to `_pendingEvents`, persisted in same transaction via `SaveChangesAsync`. For notification/email side-effects driven by handler actions on non-AggregateRoot entities.

**Why two mechanisms:** Identity entities (Organization, OrganizationUser) are structurally incompatible with `AggregateRoot` (Organization doesn't need `OrganizationId`; OrganizationUser is a join entity). Making them AggregateRoots just to raise events is wrong motivation. The handler-level mechanism complements the existing pattern without breaking entity hierarchy.

### Events carry only IDs, not data

Event payloads contain only `AggregateId` and `OrganizationId`. Handlers query the database for current data (email, name, org name) at processing time. This keeps events small and avoids stale data.

### AggregateId semantics per event type

`AggregateId` represents the primary entity affected by the event:

| Event prefix | AggregateId contains | Why |
|---|---|---|
| `member.*` | `UserId` (internal UUID) | OrganizationUser has composite PK — UserId is the meaningful identifier. Combined with `OrganizationId` from the event, uniquely identifies the membership. |
| `invite.*` | `InviteId` (internal UUID) | The invite record is the primary entity. |
| `organization.*` | `OrganizationId` (internal UUID) | The organization itself. `OrganizationId` field on the event will also be the same value. |

### Email via IEmailTemplateService

Email content is not embedded in handlers. `IEmailTemplateService` (interface in Application, implementation in Infrastructure) returns `EmailMessage` records. Handlers call the template service then `IEmailService.SendAsync`. This separates "what triggers email" from "what the email says."

### Retry strategy

Outbox handlers throw exceptions on failure. The existing Quartz job catches exceptions and applies exponential backoff (2^retryCount minutes, max 5 retries, then dead-letter). No changes to retry infrastructure needed.

### Webhook handler context

`HandleMembershipCreatedHandler` (Clerk webhook) runs without authenticated user context — `IUserContext.UserId` is `Guid.Empty`. The existing `PersistDomainEventsToOutbox` writes `createdById` from `IUserContext.UserId`, so outbox messages from webhook handlers will have `created_by_id = NULL`. This is acceptable for notification events — the email recipient is determined from event data, not from who triggered it.

## Cleanup

### Remove existing unused events and AggregateRoot demotion

- **Delete** all Contact events: `ContactCreatedEvent`, `ContactUpdatedEvent`, `ContactArchivedEvent`, `ContactUnarchivedEvent`, `ContactDeletedEvent`
- **Delete** all Property events: `PropertyCreatedEvent`, `PropertyUpdatedEvent`
- **Delete** unused Identity events: `MembershipCreatedEvent`, `MembershipRemovedEvent`
- **Delete** `MarkAs*` methods from `Contact` and `Property` entities
- **Remove** `MarkAs*` calls from command handlers that reference them
- **Demote** `Contact` and `Property` from `AggregateRoot` to `OrganizationEntity` — they no longer raise domain events. Can be promoted back when business domain events are needed.

## New Components

### 1. Deserialize helper on OutboxJsonOptions

Add a static `Deserialize<T>` method to the existing `OutboxJsonOptions` class:

```csharp
public static T Deserialize<T>(string json) =>
    JsonSerializer.Deserialize<T>(json, Instance)
    ?? throw new InvalidOperationException($"Failed to deserialize outbox event payload to {typeof(T).Name}");
```

This ensures serialization/deserialization uses the same `CamelCase` policy.

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

Located in `Application/Common/Messaging/`. DI registration continues to work — `AddOutboxEventHandlers` scans the Application assembly for `IOutboxEventHandler` implementations, which the base class satisfies.

### 3. Domain Events (8 total)

All in `Domain/Entities/Identity/Events/`:

| Event Record | EventType String | Raised By |
|---|---|---|
| `MemberDeactivatedEvent` | `member.deactivated` | `DeactivateMemberHandler` (both admin and org-scoped) |
| `MemberReactivatedEvent` | `member.reactivated` | `ReactivateMemberHandler` (both admin and org-scoped) |
| `MemberRemovedEvent` | `member.removed` | `RemoveMemberHandler` |
| `MemberRolesUpdatedEvent` | `member.roles_updated` | `UpdateMemberRolesHandler` |
| `InviteAcceptedEvent` | `invite.accepted` | `HandleMembershipCreatedHandler` (Clerk webhook — invite accepted on membership create) |
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
Application/Features/
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

| Event | AggregateId | Recipient(s) | Handler Query Strategy |
|---|---|---|---|
| `member.deactivated` | UserId | Deactivated member | `users` by `id = AggregateId` for email/name; `organizations` by `id = OrganizationId` for org name |
| `member.reactivated` | UserId | Reactivated member | Same as above |
| `member.removed` | UserId | Removed member | Same as above. Note: `organization_users` row is hard-deleted, but `users` row persists. Query `users` directly, not through membership join. If user is deactivated (`is_active = false`), use raw Dapper query against `users` table (bypasses EF global filters). |
| `member.roles_updated` | UserId | Member whose roles changed | `users` by `id = AggregateId`; `organization_user_roles` + `roles` by `user_id + organization_id` for current role names. Note: email shows roles at processing time, not at event time — acceptable since processing is near-immediate. |
| `invite.accepted` | InviteId | Accepted user | `invites` by `id = AggregateId` for invitee email; `organizations` by `id = OrganizationId` for org name |
| `invite.canceled` | InviteId | Invitee | `invites` by `id = AggregateId` for email; `organizations` by `id = OrganizationId` for org name |
| `organization.deactivated` | OrganizationId | All org members | `organizations` by `id = AggregateId` for org name; `organization_users` + `users` by `organization_id` for member emails. Query must NOT filter by membership status — org deactivation sets all members to deactivated in the same transaction, so by processing time all are inactive. |
| `organization.reactivated` | OrganizationId | All org members | Same as above — members stay deactivated on org reactivate (per business rule), so query must include all members regardless of status. |

## Testing Strategy

- Unit tests for `OutboxEventHandler<TEvent>` base class deserialization
- Unit tests for `EmailTemplateService` — each method returns correct `EmailMessage`
- Integration tests for at least one end-to-end flow: command → outbox message persisted → verify outbox row exists with correct event type and payload

## Files Changed Summary

### New files
- `Domain/Entities/Identity/Events/` — 8 event records
- `Application/Common/Messaging/OutboxEventHandler.cs` — generic base class
- `Application/Features/Members/EventHandlers/` — 4 handlers
- `Application/Features/Invites/EventHandlers/` — 2 handlers
- `Application/Features/Organizations/EventHandlers/` — 2 handlers
- `Application/Common/Interfaces/Email/IEmailTemplateService.cs`
- `Infrastructure/Email/EmailTemplateService.cs`

### Modified files
- `Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs` — add `Deserialize<T>` helper
- Command handlers (8 handlers) — add `dbContext.AddOutboxEvent(...)` calls
- `Domain/Entities/Contacts/Contact.cs` — remove `MarkAs*` methods, demote to `OrganizationEntity`
- `Domain/Entities/Properties/Property.cs` — remove `MarkAs*` methods, demote to `OrganizationEntity`
- Handlers referencing `MarkAs*` — remove those calls

### Deleted files
- `Domain/Entities/Contacts/Events/` — all 5 event files
- `Domain/Entities/Properties/Events/` — all 2 event files
- `Domain/Entities/Identity/Events/MembershipCreatedEvent.cs`
- `Domain/Entities/Identity/Events/MembershipRemovedEvent.cs`
