# Domain Events & Outbox Handlers Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up domain event raising in identity command handlers and implement outbox email notification handlers for member, invite, and organization lifecycle events.

**Architecture:** Handler-level side-effect events via `IAppDbContext.AddOutboxEvent()` (identity entities aren't AggregateRoots). Events carry only IDs — handlers query DB for current data at processing time via `IDbConnectionFactory` (same pattern as all existing Dapper queries). Email content delegated to `IEmailTemplateService`.

**Tech Stack:** .NET 10, MediatR, Dapper, Quartz outbox, Resend email, xUnit/FluentAssertions/NSubstitute

**Spec:** `docs/superpowers/specs/2026-03-17-domain-events-outbox-design.md`

---

## File Map

### New Files

| File | Responsibility |
|---|---|
| `src/Propentro.Domain/Entities/Identity/Events/MemberDeactivatedEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/MemberReactivatedEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/MemberRemovedEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/MemberRolesUpdatedEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/InviteAcceptedEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/InviteCanceledEvent.cs` | Domain event record |
| `src/Propentro.Domain/Entities/Identity/Events/OrganizationDeactivatedEvent.cs` | Domain event record |
| `src/Propentro.Application/Common/Messaging/OutboxEventHandler.cs` | Generic base class for typed deserialization |
| `src/Propentro.Application/Common/Interfaces/Email/IEmailTemplateService.cs` | Interface — email content per event type |
| `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandler.cs` | Outbox handler — sends email to deactivated member |
| `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandler.cs` | Outbox handler — sends email to reactivated member |
| `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandler.cs` | Outbox handler — sends email to removed member |
| `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandler.cs` | Outbox handler — sends email to member with updated roles |
| `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandler.cs` | Outbox handler — sends welcome email to accepted invite |
| `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandler.cs` | Outbox handler — sends email to canceled invitee |
| `src/Propentro.Application/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandler.cs` | Outbox handler — sends email to all org members |
| `src/Propentro.Infrastructure/Email/EmailTemplateService.cs` | Implementation — inline HTML templates |
| `tests/Propentro.Application.Tests/Common/Messaging/OutboxEventHandlerTests.cs` | Unit tests for base class deserialization |
| `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandlerTests.cs` | Unit test for member deactivated handler |
| `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandlerTests.cs` | Unit test for invite accepted handler |
| `tests/Propentro.Application.Tests/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandlerTests.cs` | Unit test for org deactivated handler |
| `tests/Propentro.Infrastructure.Tests/Email/EmailTemplateServiceTests.cs` | Unit tests for email template service |

### Modified Files

| File | Change |
|---|---|
| `src/Propentro.Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs` | Add `Deserialize<T>` static method |
| `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberDeactivatedEvent(...))` |
| `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberDeactivatedEvent(...))` |
| `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberReactivatedEvent(...))` |
| `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberReactivatedEvent(...))` |
| `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberRemovedEvent(...))` |
| `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesHandler.cs` | Add `dbContext.AddOutboxEvent(new MemberRolesUpdatedEvent(...))` |
| `src/Propentro.Application/Features/Webhooks/Commands/HandleMembershipCreated/HandleMembershipCreatedHandler.cs` | Add `dbContext.AddOutboxEvent(new InviteAcceptedEvent(...))` |
| `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteHandler.cs` | Add `dbContext.AddOutboxEvent(new InviteCanceledEvent(...))` |
| `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs` | Add `dbContext.AddOutboxEvent(new OrganizationDeactivatedEvent(...))` |
| `src/Propentro.Infrastructure/DependencyInjection.cs` | Register `EmailTemplateService` as Singleton |
| `src/Propentro.Domain/Entities/Contacts/Contact.cs` | Remove `MarkAs*` methods, demote from `AggregateRoot` to `OrganizationEntity` |
| `src/Propentro.Domain/Entities/Properties/Property.cs` | Remove `MarkAs*` methods, demote from `AggregateRoot` to `OrganizationEntity` |

### Deleted Files

| File | Reason |
|---|---|
| `src/Propentro.Domain/Entities/Contacts/Events/ContactCreatedEvent.cs` | Unused — no handlers, no callers |
| `src/Propentro.Domain/Entities/Contacts/Events/ContactUpdatedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Contacts/Events/ContactArchivedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Contacts/Events/ContactUnarchivedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Contacts/Events/ContactDeletedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Properties/Events/PropertyCreatedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Properties/Events/PropertyUpdatedEvent.cs` | Unused |
| `src/Propentro.Domain/Entities/Identity/Events/MembershipCreatedEvent.cs` | Replaced by new events |
| `src/Propentro.Domain/Entities/Identity/Events/MembershipRemovedEvent.cs` | Replaced by `MemberRemovedEvent` |

### DI Verification

- **Handler discovery:** `AddOutboxEventHandlers()` in `Infrastructure/DependencyInjection.cs:188-199` scans `typeof(IOutboxEventHandler).Assembly` which is `Propentro.Application`. All 7 new handlers live in the Application assembly — they will be discovered automatically.
- **Handler lifetime:** Handlers are registered as `Transient`, but `ProcessOutboxMessagesJob` caches them in a dictionary at construction time (constructor injection of `IEnumerable<IOutboxEventHandler>`). This means handlers are effectively singleton per job scope. All handler dependencies must be stateless or create connections on demand.
- **Dependencies already registered:** `IDbConnectionFactory` (Singleton), `IEmailService` (HttpClient/Transient), `IEmailTemplateService` (Singleton — registered in this plan).

### Known Limitations

1. **Org deactivation handler carries member IDs in event payload.** `OrganizationDeactivatedEvent` includes `MemberUserIds` captured before hard delete. The outbox handler queries users directly by ID — no join through `organization_users` needed.
2. **No org reactivation event.** `DeactivateOrganizationHandler` hard-deletes `organization_users` rows. On reactivation, no members exist to notify — they must be re-invited. Org reactivation email can be added later if deactivation is changed to soft-delete memberships.
3. **Non-invite membership paths skip events.** Users added directly through Clerk dashboard (bypassing invite flow) trigger the `HandleMembershipCreatedHandler` webhook but hit the `pendingInvite is null` early return — no membership is created and no event is raised. This is correct behavior (no local membership = no event).

---

## Task 1: Cleanup — Delete Unused Events & Demote Entities

No dependencies. Removes dead code before adding new code.

**Files:**
- Delete: All 5 files in `src/Propentro.Domain/Entities/Contacts/Events/`
- Delete: All 2 files in `src/Propentro.Domain/Entities/Properties/Events/`
- Delete: `src/Propentro.Domain/Entities/Identity/Events/MembershipCreatedEvent.cs`
- Delete: `src/Propentro.Domain/Entities/Identity/Events/MembershipRemovedEvent.cs`
- Modify: `src/Propentro.Domain/Entities/Contacts/Contact.cs`
- Modify: `src/Propentro.Domain/Entities/Properties/Property.cs`

- [ ] **Step 1: Delete all Contact event files and directory**

```bash
rm src/Propentro.Domain/Entities/Contacts/Events/ContactCreatedEvent.cs \
   src/Propentro.Domain/Entities/Contacts/Events/ContactUpdatedEvent.cs \
   src/Propentro.Domain/Entities/Contacts/Events/ContactArchivedEvent.cs \
   src/Propentro.Domain/Entities/Contacts/Events/ContactUnarchivedEvent.cs \
   src/Propentro.Domain/Entities/Contacts/Events/ContactDeletedEvent.cs
rmdir src/Propentro.Domain/Entities/Contacts/Events
```

- [ ] **Step 2: Delete all Property event files and directory**

```bash
rm src/Propentro.Domain/Entities/Properties/Events/PropertyCreatedEvent.cs \
   src/Propentro.Domain/Entities/Properties/Events/PropertyUpdatedEvent.cs
rmdir src/Propentro.Domain/Entities/Properties/Events
```

- [ ] **Step 3: Delete old Identity events**

```bash
rm src/Propentro.Domain/Entities/Identity/Events/MembershipCreatedEvent.cs \
   src/Propentro.Domain/Entities/Identity/Events/MembershipRemovedEvent.cs
```

Do NOT delete the `Events/` directory — new event files go here in Task 3.

- [ ] **Step 4: Demote Contact from AggregateRoot to OrganizationEntity**

In `src/Propentro.Domain/Entities/Contacts/Contact.cs`:

Remove the `using Propentro.Domain.Entities.Contacts.Events;` import (line 3).

Change base class from `AggregateRoot` to `OrganizationEntity` (line 8):
```csharp
public sealed class Contact : OrganizationEntity
```

Delete all 5 `MarkAs*` methods (lines 70–74):
```csharp
    // DELETE these lines:
    public void MarkAsCreated(Guid organizationId) => RaiseDomainEvent(new ContactCreatedEvent(Id, organizationId));
    public void MarkAsUpdated(Guid organizationId) => RaiseDomainEvent(new ContactUpdatedEvent(Id, organizationId));
    public void MarkAsArchived(Guid organizationId) => RaiseDomainEvent(new ContactArchivedEvent(Id, organizationId));
    public void MarkAsUnarchived(Guid organizationId) => RaiseDomainEvent(new ContactUnarchivedEvent(Id, organizationId));
    public void MarkAsDeleted(Guid organizationId) => RaiseDomainEvent(new ContactDeletedEvent(Id, organizationId));
```

The constructor `base(PublicIdPrefix)` still works — `OrganizationEntity` has the same `(string prefix)` constructor.

- [ ] **Step 5: Demote Property from AggregateRoot to OrganizationEntity**

In `src/Propentro.Domain/Entities/Properties/Property.cs`:

Remove the `using Propentro.Domain.Entities.Properties.Events;` import (line 3).

Change base class from `AggregateRoot` to `OrganizationEntity` (line 8):
```csharp
public sealed class Property : OrganizationEntity
```

Delete both `MarkAs*` methods (lines 41–42):
```csharp
    // DELETE these lines:
    public void MarkAsCreated(Guid organizationId) => RaiseDomainEvent(new PropertyCreatedEvent(Id, organizationId));
    public void MarkAsUpdated(Guid organizationId) => RaiseDomainEvent(new PropertyUpdatedEvent(Id, organizationId));
```

- [ ] **Step 6: Build to verify no broken references**

```bash
dotnet build Propentro.slnx
```

Expected: SUCCESS. No handlers call `MarkAs*` methods — grep confirmed zero references in Application layer. `PersistDomainEventsToOutbox` scans `ChangeTracker.Entries<AggregateRoot>()`, which will now skip Contact/Property — correct, they have no events.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: delete unused domain events and demote Contact/Property to OrganizationEntity

Contact and Property entities had MarkAs* methods and event records
that were never consumed by any handler. Remove them and demote
both entities from AggregateRoot to OrganizationEntity. They can be
promoted back when business domain events are actually needed."
```

---

## Task 2: OutboxJsonOptions.Deserialize + OutboxEventHandler Base Class

Foundation for all outbox handlers. No external dependencies.

**Files:**
- Modify: `src/Propentro.Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs`
- Create: `src/Propentro.Application/Common/Messaging/OutboxEventHandler.cs`
- Create: `tests/Propentro.Application.Tests/Common/Messaging/OutboxEventHandlerTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Propentro.Application.Tests/Common/Messaging/OutboxEventHandlerTests.cs`:

```csharp
using FluentAssertions;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Common;

namespace Propentro.Application.Tests.Common.Messaging;

public class OutboxJsonOptionsTests
{
    [Fact]
    public void Deserialize_Should_Return_Object_From_Valid_Json()
    {
        var json = """{"name":"test","value":42}""";

        var result = OutboxJsonOptions.Deserialize<TestPayload>(json);

        result.Should().NotBeNull();
        result.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_Should_Handle_CamelCase_Properties()
    {
        var json = """{"firstName":"John","lastName":"Doe"}""";

        var result = OutboxJsonOptions.Deserialize<CamelCasePayload>(json);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Deserialize_Should_Throw_On_Null_Result()
    {
        var json = "null";

        var act = () => OutboxJsonOptions.Deserialize<TestPayload>(json);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestPayload*");
    }

    private sealed record TestPayload(string Name, int Value);
    private sealed record CamelCasePayload(string FirstName, string LastName);
}

public class OutboxEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Deserialize_Payload_And_Invoke_Typed_Handler()
    {
        var handler = new TestEventHandler();
        var payload = """{"aggregateId":"11111111-1111-1111-1111-111111111111","organizationId":"22222222-2222-2222-2222-222222222222"}""";

        await ((IOutboxEventHandler)handler).HandleAsync(payload);

        handler.ReceivedEvent.Should().NotBeNull();
        handler.ReceivedEvent!.AggregateId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        handler.ReceivedEvent.OrganizationId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public void EventType_Should_Match_Concrete_Handler()
    {
        var handler = new TestEventHandler();

        handler.EventType.Should().Be("test.event");
    }

    private sealed record TestEvent(Guid AggregateId, Guid OrganizationId)
        : DomainEvent(AggregateId, OrganizationId)
    {
        public override string EventType => "test.event";
    }

    private sealed class TestEventHandler : OutboxEventHandler<TestEvent>
    {
        public TestEvent? ReceivedEvent { get; private set; }

        public override string EventType => "test.event";

        protected override Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvent = @event;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~OutboxJsonOptionsTests or FullyQualifiedName~OutboxEventHandlerTests" -v minimal
```

Expected: FAIL — `Deserialize` method and `OutboxEventHandler<T>` class don't exist.

- [ ] **Step 3: Implement OutboxJsonOptions.Deserialize**

In `src/Propentro.Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs`:

```csharp
using System.Text.Json;

namespace Propentro.Application.Common.Interfaces.Messaging;

public static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Instance)
        ?? throw new InvalidOperationException($"Failed to deserialize outbox event payload to {typeof(T).Name}");
}
```

- [ ] **Step 4: Implement OutboxEventHandler base class**

Create `src/Propentro.Application/Common/Messaging/OutboxEventHandler.cs`:

```csharp
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Application.Common.Messaging;

public abstract class OutboxEventHandler<TEvent> : IOutboxEventHandler
    where TEvent : IDomainEvent
{
    public abstract string EventType { get; }

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var @event = OutboxJsonOptions.Deserialize<TEvent>(payload);
        return HandleAsync(@event, cancellationToken);
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
```

- [ ] **Step 5: Run all tests to verify passes**

```bash
dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~OutboxJsonOptionsTests or FullyQualifiedName~OutboxEventHandlerTests" -v minimal
```

Expected: PASS (5 tests)

- [ ] **Step 6: Build entire solution**

```bash
dotnet build Propentro.slnx
```

Expected: SUCCESS

- [ ] **Step 7: Commit**

```bash
git add src/Propentro.Application/Common/Interfaces/Messaging/OutboxJsonOptions.cs \
        src/Propentro.Application/Common/Messaging/OutboxEventHandler.cs \
        tests/Propentro.Application.Tests/Common/Messaging/OutboxEventHandlerTests.cs
git commit -m "feat: add OutboxJsonOptions.Deserialize and OutboxEventHandler<T> base class

Typed deserialization helper ensures consistent camelCase policy.
Generic base class handles deserialization so concrete handlers
receive strongly-typed domain events."
```

---

## Task 3: Create 7 New Domain Event Records

All follow the same pattern. No logic — just data records.

**Files:**
- Create: 7 files in `src/Propentro.Domain/Entities/Identity/Events/`

- [ ] **Step 1: Create all 7 event records**

Each file follows this exact pattern (showing one as template):

`MemberDeactivatedEvent.cs`:
```csharp
using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record MemberDeactivatedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.deactivated";
}
```

Create all 8 files with these event types:

| File | Record Name | EventType |
|---|---|---|
| `MemberDeactivatedEvent.cs` | `MemberDeactivatedEvent` | `"member.deactivated"` |
| `MemberReactivatedEvent.cs` | `MemberReactivatedEvent` | `"member.reactivated"` |
| `MemberRemovedEvent.cs` | `MemberRemovedEvent` | `"member.removed"` |
| `MemberRolesUpdatedEvent.cs` | `MemberRolesUpdatedEvent` | `"member.roles_updated"` |
| `InviteAcceptedEvent.cs` | `InviteAcceptedEvent` | `"invite.accepted"` |
| `InviteCanceledEvent.cs` | `InviteCanceledEvent` | `"invite.canceled"` |
| `OrganizationDeactivatedEvent.cs` | `OrganizationDeactivatedEvent` | `"organization.deactivated"` |

All use `sealed record`, all inherit from `DomainEvent(AggregateId, OrganizationId)`, all have `using Propentro.Domain.Common;` and namespace `Propentro.Domain.Entities.Identity.Events;`.

**Exception:** `OrganizationDeactivatedEvent` carries additional data — member user IDs captured before hard delete:

```csharp
using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record OrganizationDeactivatedEvent(
    Guid AggregateId,
    Guid OrganizationId,
    IReadOnlyList<Guid> MemberUserIds) : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "organization.deactivated";
}
```

This is the only event that carries data beyond IDs. `DeactivateOrganizationHandler` hard-deletes `organization_users` rows in the same transaction, so member IDs must be captured before deletion and embedded in the event payload. The outbox handler queries `users` directly by these IDs instead of joining through `organization_users`.

- [ ] **Step 2: Build to verify**

```bash
dotnet build Propentro.slnx
```

Expected: SUCCESS

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Domain/Entities/Identity/Events/
git commit -m "feat: add 7 identity domain event records

Member (deactivated, reactivated, removed, roles_updated),
Invite (accepted, canceled), Organization (deactivated).
AggregateId semantics: UserId for member.*, InviteId for invite.*,
OrganizationId for organization.*. OrganizationDeactivatedEvent
carries MemberUserIds for post-hard-delete email delivery."
```

---

## Task 4: Wire Events into Command Handlers

Add `dbContext.AddOutboxEvent(...)` calls to 9 command handlers. Each call goes right before `return Result.Success()`. The event is added to `_pendingEvents` and persisted in the same `SaveChangesAsync` transaction.

**Files:**
- Modify: 9 handler files (see File Map above)

- [ ] **Step 1: Add event to DeactivateMemberHandler (org-scoped)**

In `src/Propentro.Application/Features/Organizations/Members/Commands/DeactivateMember/DeactivateMemberHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after the permission cache invalidation on line 92):
```csharp
        dbContext.AddOutboxEvent(new MemberDeactivatedEvent(user.Id, currentUser.OrganizationId));
```

**AggregateId = `user.Id`** (internal UUID of the deactivated user). Combined with `OrganizationId`, uniquely identifies the membership.

- [ ] **Step 2: Add event to AdminDeactivateMemberHandler**

In `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateMember/AdminDeactivateMemberHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after the log on line 56):
```csharp
        dbContext.AddOutboxEvent(new MemberDeactivatedEvent(user.Id, org.Id));
```

- [ ] **Step 3: Add event to ReactivateMemberHandler (org-scoped)**

In `src/Propentro.Application/Features/Organizations/Members/Commands/ReactivateMember/ReactivateMemberHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after permission cache invalidation on line 50):
```csharp
        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, currentUser.OrganizationId));
```

- [ ] **Step 4: Add event to AdminReactivateMemberHandler**

In `src/Propentro.Application/Features/Admin/Users/Commands/ReactivateMember/AdminReactivateMemberHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after the log on line 47):
```csharp
        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, org.Id));
```

- [ ] **Step 5: Add event to RemoveMemberHandler**

In `src/Propentro.Application/Features/Admin/Users/Commands/RemoveMember/RemoveMemberHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after the log on line 75):
```csharp
        dbContext.AddOutboxEvent(new MemberRemovedEvent(user.Id, org.Id));
```

Note: The `organization_users` row is hard-deleted in this handler, but the user row persists. The outbox handler will query `users` directly for email/name.

- [ ] **Step 6: Add event to UpdateMemberRolesHandler**

In `src/Propentro.Application/Features/Organizations/Members/Commands/UpdateMemberRoles/UpdateMemberRolesHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after permission cache invalidation on line 101):
```csharp
        dbContext.AddOutboxEvent(new MemberRolesUpdatedEvent(user.Id, currentUser.OrganizationId));
```

- [ ] **Step 7: Add event to HandleMembershipCreatedHandler (webhook)**

In `src/Propentro.Application/Features/Webhooks/Commands/HandleMembershipCreated/HandleMembershipCreatedHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before the final `return Result.Success();` at line 125 (after the log on line 122), but NOT before the early returns at lines 63, 78, 98:
```csharp
        dbContext.AddOutboxEvent(new InviteAcceptedEvent(pendingInvite.Id, org.Id));
```

**AggregateId = `pendingInvite.Id`** (internal UUID of the invite record).

Note: This handler runs from a Clerk webhook — no authenticated user context. `IUserContext.UserId` is `Guid.Empty`, so the outbox message will have `created_by_id = NULL`. This is acceptable for notification events.

- [ ] **Step 8: Add event to CancelInviteHandler**

In `src/Propentro.Application/Features/Organizations/Invites/Commands/CancelInvite/CancelInviteHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

Add before `return Result.Success();` (after the log on line 41):
```csharp
        dbContext.AddOutboxEvent(new InviteCanceledEvent(invite.Id, invite.OrganizationId));
```

**AggregateId = `invite.Id`** (internal UUID of the invite record).

- [ ] **Step 9: Add event to DeactivateOrganizationHandler**

In `src/Propentro.Application/Features/Admin/Organizations/Commands/DeactivateOrganization/DeactivateOrganizationHandler.cs`:

Add using: `using Propentro.Domain.Entities.Identity.Events;`

The `memberUserIds` list is already computed on line 40 (`memberships.Select(ou => ou.UserId).ToList()`). This is captured **before** the hard deletes on lines 47–50.

Add before `return Result.Success();` (after the log on line 97):
```csharp
        dbContext.AddOutboxEvent(new OrganizationDeactivatedEvent(org.Id, org.Id, memberUserIds));
```

**AggregateId = `org.Id`** (same as OrganizationId). **MemberUserIds** = the list of user IDs captured before `organization_users` rows were hard-deleted. The outbox handler will use these IDs to query `users` directly.

- [ ] **Step 10: Build to verify**

```bash
dotnet build Propentro.slnx
```

Expected: SUCCESS

- [ ] **Step 11: Commit**

```bash
git add src/Propentro.Application/Features/
git commit -m "feat: raise domain events in identity command handlers

Add AddOutboxEvent calls to 9 handlers: deactivate/reactivate member
(org-scoped + admin), remove member, update roles, membership webhook,
cancel invite, deactivate org. Events persisted to outbox in same
SaveChangesAsync transaction."
```

---

## Task 5: IEmailTemplateService Interface + EmailTemplateService Implementation

**Files:**
- Create: `src/Propentro.Application/Common/Interfaces/Email/IEmailTemplateService.cs`
- Create: `src/Propentro.Infrastructure/Email/EmailTemplateService.cs`
- Create: `tests/Propentro.Infrastructure.Tests/Email/EmailTemplateServiceTests.cs`
- Modify: `src/Propentro.Infrastructure/DependencyInjection.cs`

- [ ] **Step 1: Write the failing test for EmailTemplateService**

First check if `tests/Propentro.Infrastructure.Tests/` project exists. If not, create it:

```bash
dotnet new xunit -n Propentro.Infrastructure.Tests -o tests/Propentro.Infrastructure.Tests
dotnet slnx add tests/Propentro.Infrastructure.Tests/Propentro.Infrastructure.Tests.csproj --solution Propentro.slnx
```

Add project references and packages to match existing test project pattern. In `Propentro.Infrastructure.Tests.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NSubstitute" />
</ItemGroup>
<ItemGroup>
    <ProjectReference Include="..\..\src\Propentro.Infrastructure\Propentro.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Propentro.Application\Propentro.Application.csproj" />
</ItemGroup>
```

Create `tests/Propentro.Infrastructure.Tests/Email/EmailTemplateServiceTests.cs`:

```csharp
using FluentAssertions;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Infrastructure.Email;

namespace Propentro.Infrastructure.Tests.Email;

public class EmailTemplateServiceTests
{
    private readonly IEmailTemplateService _sut = new EmailTemplateService();

    [Fact]
    public void MemberDeactivated_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberDeactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("John");
        result.HtmlBody.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("deactivated");
    }

    [Fact]
    public void MemberReactivated_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberReactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("reactivated");
    }

    [Fact]
    public void MemberRemoved_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberRemoved("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("removed");
    }

    [Fact]
    public void MemberRolesUpdated_Should_Include_Role_Names()
    {
        var result = _sut.MemberRolesUpdated("john@example.com", "John", "Acme Corp", ["Owner", "Manager"]);

        result.To.Should().Be("john@example.com");
        result.HtmlBody.Should().Contain("Owner");
        result.HtmlBody.Should().Contain("Manager");
    }

    [Fact]
    public void InviteAccepted_Should_Return_Welcome_Email()
    {
        var result = _sut.InviteAccepted("john@example.com", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
    }

    [Fact]
    public void InviteCanceled_Should_Return_Email()
    {
        var result = _sut.InviteCanceled("john@example.com", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
    }

    [Fact]
    public void OrganizationDeactivated_Should_Return_Email()
    {
        var result = _sut.OrganizationDeactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.HtmlBody.Should().Contain("deactivated");
        result.HtmlBody.Should().Contain("Acme Corp");
    }

}
```

- [ ] **Step 2: Create IEmailTemplateService interface**

Create `src/Propentro.Application/Common/Interfaces/Email/IEmailTemplateService.cs`:

```csharp
namespace Propentro.Application.Common.Interfaces.Email;

public interface IEmailTemplateService
{
    EmailMessage MemberDeactivated(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberReactivated(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberRemoved(string recipientEmail, string recipientName, string organizationName);
    EmailMessage MemberRolesUpdated(string recipientEmail, string recipientName, string organizationName, IReadOnlyList<string> roleNames);
    EmailMessage InviteAccepted(string recipientEmail, string organizationName);
    EmailMessage InviteCanceled(string recipientEmail, string organizationName);
    EmailMessage OrganizationDeactivated(string recipientEmail, string recipientName, string organizationName);
}
```

Note: `EmailMessage` is already defined in the same namespace (`Application.Common.Interfaces.Email`) — no additional import needed.

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test tests/Propentro.Infrastructure.Tests --filter "FullyQualifiedName~EmailTemplateServiceTests" -v minimal
```

Expected: FAIL — `EmailTemplateService` class doesn't exist.

- [ ] **Step 4: Implement EmailTemplateService**

Create `src/Propentro.Infrastructure/Email/EmailTemplateService.cs`:

```csharp
using Propentro.Application.Common.Interfaces.Email;

namespace Propentro.Infrastructure.Email;

public sealed class EmailTemplateService : IEmailTemplateService
{
    public EmailMessage MemberDeactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your membership in {organizationName} has been deactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your membership in <strong>{organizationName}</strong> has been deactivated. You will no longer have access to this organization's resources.</p>
                <p>If you believe this was a mistake, please contact your organization administrator.</p>
                """);

    public EmailMessage MemberReactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your membership in {organizationName} has been reactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your membership in <strong>{organizationName}</strong> has been reactivated. You can now access this organization's resources again.</p>
                """);

    public EmailMessage MemberRemoved(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"You have been removed from {organizationName}",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>You have been removed from <strong>{organizationName}</strong>. You will no longer have access to this organization's resources.</p>
                <p>If you believe this was a mistake, please contact the organization administrator.</p>
                """);

    public EmailMessage MemberRolesUpdated(string recipientEmail, string recipientName, string organizationName, IReadOnlyList<string> roleNames) =>
        new(
            To: recipientEmail,
            Subject: $"Your roles in {organizationName} have been updated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>Your roles in <strong>{organizationName}</strong> have been updated. Your current roles are:</p>
                <ul>{string.Join("", roleNames.Select(r => $"<li>{r}</li>"))}</ul>
                """);

    public EmailMessage InviteAccepted(string recipientEmail, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Welcome to {organizationName}",
            HtmlBody: $"""
                <p>Welcome!</p>
                <p>Your invitation to join <strong>{organizationName}</strong> has been accepted. You now have access to the organization's resources.</p>
                """);

    public EmailMessage InviteCanceled(string recipientEmail, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"Your invitation to {organizationName} has been canceled",
            HtmlBody: $"""
                <p>The invitation for you to join <strong>{organizationName}</strong> has been canceled.</p>
                <p>If you have questions, please contact the organization administrator.</p>
                """);

    public EmailMessage OrganizationDeactivated(string recipientEmail, string recipientName, string organizationName) =>
        new(
            To: recipientEmail,
            Subject: $"{organizationName} has been deactivated",
            HtmlBody: $"""
                <p>Hi {recipientName},</p>
                <p>The organization <strong>{organizationName}</strong> has been deactivated. All members have lost access to this organization's resources.</p>
                <p>If you have questions, please contact your system administrator.</p>
                """);

}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/Propentro.Infrastructure.Tests --filter "FullyQualifiedName~EmailTemplateServiceTests" -v minimal
```

Expected: PASS (7 tests)

- [ ] **Step 6: Register EmailTemplateService in DI**

In `src/Propentro.Infrastructure/DependencyInjection.cs`, add inside the `AddEmail` method alongside the existing `IEmailService` registration:

```csharp
services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
```

`Singleton` is appropriate — `EmailTemplateService` has no state or dependencies. This also works correctly with the `ProcessOutboxMessagesJob` handler caching (see DI Verification in File Map).

Add using: `using Propentro.Infrastructure.Email;` if not already present (check if `ResendEmailService` registration already brings it in).

- [ ] **Step 7: Build entire solution**

```bash
dotnet build Propentro.slnx
```

Expected: SUCCESS

- [ ] **Step 8: Commit**

```bash
git add src/Propentro.Application/Common/Interfaces/Email/IEmailTemplateService.cs \
        src/Propentro.Infrastructure/Email/EmailTemplateService.cs \
        src/Propentro.Infrastructure/DependencyInjection.cs \
        tests/Propentro.Infrastructure.Tests/
git commit -m "feat: add IEmailTemplateService with inline HTML templates

Interface in Application layer, implementation in Infrastructure.
7 methods for member/invite/org lifecycle email content.
Registered as Singleton — no state or dependencies."
```

---

## Task 6: Member Event Handlers (4 handlers)

All member event handlers inject `IDbConnectionFactory` (Singleton, already registered), create a connection per invocation, query via Dapper, then send email. This matches the pattern used by every query handler in the codebase (e.g., `GetMemberHandler`).

**Files:**
- Create: `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandler.cs`
- Create: `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandler.cs`
- Create: `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandler.cs`
- Create: `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandler.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandlerTests.cs`

- [ ] **Step 1: Write the failing test for MemberDeactivatedEventHandler**

Create `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandlerTests.cs`:

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberDeactivatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberDeactivatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.deactivated");
    }
}
```

- [ ] **Step 2: Implement MemberDeactivatedEventHandler**

Create `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberDeactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberDeactivatedEvent>
{
    public override string EventType => "member.deactivated";

    protected override async Task HandleAsync(MemberDeactivatedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var recipient = await connection.QuerySingleOrDefaultAsync<RecipientInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = @UserId
            """,
            new { UserId = @event.AggregateId });

        if (recipient is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.MemberDeactivated(recipient.Email, recipient.Name ?? "there", orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.deactivated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
```

**Key design decisions:**
- `IDbConnectionFactory` creates a fresh connection per handler invocation — safe for the cached-handler pattern in `ProcessOutboxMessagesJob`.
- Raw Dapper bypasses EF Core global filters (deactivated users have `is_active = false`).
- Returns silently if user/org not found (data may have been deleted between event creation and processing).
- Throws on email send failure — outbox retries with exponential backoff.
- `RecipientInfo` is a non-positional `sealed record` with `init` properties (Dapper best practice per CLAUDE.md).

- [ ] **Step 3: Implement MemberReactivatedEventHandler**

Create `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberReactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberReactivatedEvent>
{
    public override string EventType => "member.reactivated";

    protected override async Task HandleAsync(MemberReactivatedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var recipient = await connection.QuerySingleOrDefaultAsync<RecipientInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = @UserId
            """,
            new { UserId = @event.AggregateId });

        if (recipient is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.MemberReactivated(recipient.Email, recipient.Name ?? "there", orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.reactivated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
```

- [ ] **Step 4: Implement MemberRemovedEventHandler**

Create `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberRemovedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberRemovedEvent>
{
    public override string EventType => "member.removed";

    protected override async Task HandleAsync(MemberRemovedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // organization_users row is hard-deleted by RemoveMemberHandler — query users directly
        var recipient = await connection.QuerySingleOrDefaultAsync<RecipientInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = @UserId
            """,
            new { UserId = @event.AggregateId });

        if (recipient is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.MemberRemoved(recipient.Email, recipient.Name ?? "there", orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.removed email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
```

- [ ] **Step 5: Implement MemberRolesUpdatedEventHandler**

Create `src/Propentro.Application/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.EventHandlers;

public sealed class MemberRolesUpdatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<MemberRolesUpdatedEvent>
{
    public override string EventType => "member.roles_updated";

    protected override async Task HandleAsync(MemberRolesUpdatedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var recipient = await connection.QuerySingleOrDefaultAsync<RecipientInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = @UserId
            """,
            new { UserId = @event.AggregateId });

        if (recipient is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var roleNames = (await connection.QueryAsync<string>(
            """
            SELECT r.name
            FROM organization_user_roles our
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = @UserId AND our.organization_id = @OrgId
            """,
            new { UserId = @event.AggregateId, OrgId = @event.OrganizationId })).ToList();

        if (roleNames.Count == 0)
            return;

        var email = templateService.MemberRolesUpdated(recipient.Email, recipient.Name ?? "there", orgName, roleNames);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send member.roles_updated email to {recipient.Email}: {result.Error.Code}");
    }

    private sealed record RecipientInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
```

Note: Roles are queried at processing time, not event time — shows current roles. Acceptable since processing is near-immediate.

- [ ] **Step 6: Build and run tests**

```bash
dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~MemberDeactivatedEventHandlerTests" -v minimal
```

Expected: BUILD SUCCESS, PASS (1 test)

- [ ] **Step 7: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Members/EventHandlers/ \
        tests/Propentro.Application.Tests/Features/Organizations/Members/
git commit -m "feat: add 4 member outbox event handlers

MemberDeactivated, MemberReactivated, MemberRemoved, MemberRolesUpdated.
Each creates a fresh IDbConnectionFactory connection per invocation,
queries users/orgs via raw Dapper (bypasses global filters),
builds email via IEmailTemplateService, sends via IEmailService.
Throws on failure for outbox retry."
```

---

## Task 7: Invite Event Handlers (2 handlers)

**Files:**
- Create: `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandler.cs`
- Create: `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandler.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandlerTests.cs`

- [ ] **Step 1: Write the failing test for InviteAcceptedEventHandler**

Create `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandlerTests.cs`:

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Invites.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Invites.EventHandlers;

public class InviteAcceptedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_InviteAccepted()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new InviteAcceptedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("invite.accepted");
    }
}
```

- [ ] **Step 2: Implement InviteAcceptedEventHandler**

Create `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Invites.EventHandlers;

public sealed class InviteAcceptedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<InviteAcceptedEvent>
{
    public override string EventType => "invite.accepted";

    protected override async Task HandleAsync(InviteAcceptedEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var inviteEmail = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT email FROM invites WHERE id = @InviteId",
            new { InviteId = @event.AggregateId });

        if (inviteEmail is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.InviteAccepted(inviteEmail, orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send invite.accepted email to {inviteEmail}: {result.Error.Code}");
    }
}
```

- [ ] **Step 3: Implement InviteCanceledEventHandler**

Create `src/Propentro.Application/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Invites.EventHandlers;

public sealed class InviteCanceledEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<InviteCanceledEvent>
{
    public override string EventType => "invite.canceled";

    protected override async Task HandleAsync(InviteCanceledEvent @event, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var inviteEmail = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT email FROM invites WHERE id = @InviteId",
            new { InviteId = @event.AggregateId });

        if (inviteEmail is null)
            return;

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        var email = templateService.InviteCanceled(inviteEmail, orgName);
        var result = await emailService.SendAsync(email, cancellationToken);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to send invite.canceled email to {inviteEmail}: {result.Error.Code}");
    }
}
```

- [ ] **Step 4: Build and run tests**

```bash
dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~InviteAcceptedEventHandlerTests" -v minimal
```

Expected: BUILD SUCCESS, PASS (1 test)

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Invites/EventHandlers/ \
        tests/Propentro.Application.Tests/Features/Organizations/Invites/
git commit -m "feat: add invite accepted and canceled outbox event handlers

InviteAccepted sends welcome email to invitee.
InviteCanceled notifies invitee that their invitation was withdrawn.
Both query invites table by AggregateId for recipient email."
```

---

## Task 8: Organization Deactivated Event Handler

Sends emails to **all org members** — different pattern from member handlers. `OrganizationDeactivatedEvent` carries `MemberUserIds` in its payload (captured before hard delete). No reactivation event — see Known Limitations.

**Files:**
- Create: `src/Propentro.Application/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandler.cs`
- Create: `tests/Propentro.Application.Tests/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandlerTests.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/Propentro.Application.Tests/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandlerTests.cs`:

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Admin.Organizations.EventHandlers;

namespace Propentro.Application.Tests.Features.Admin.Organizations.EventHandlers;

public class OrganizationDeactivatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_OrganizationDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new OrganizationDeactivatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("organization.deactivated");
    }
}
```

- [ ] **Step 2: Implement OrganizationDeactivatedEventHandler**

Create `src/Propentro.Application/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandler.cs`:

```csharp
using Dapper;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Organizations.EventHandlers;

public sealed class OrganizationDeactivatedEventHandler(
    IDbConnectionFactory db,
    IEmailService emailService,
    IEmailTemplateService templateService) : OutboxEventHandler<OrganizationDeactivatedEvent>
{
    public override string EventType => "organization.deactivated";

    protected override async Task HandleAsync(OrganizationDeactivatedEvent @event, CancellationToken cancellationToken)
    {
        if (@event.MemberUserIds.Count == 0)
            return;

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var orgName = await connection.QuerySingleOrDefaultAsync<string>(
            "SELECT name FROM organizations WHERE id = @OrgId",
            new { OrgId = @event.OrganizationId });

        if (orgName is null)
            return;

        // Query users directly by IDs from event payload — organization_users rows
        // are hard-deleted by DeactivateOrganizationHandler, so we can't join through them.
        // MemberUserIds were captured before deletion and embedded in the event.
        var members = await connection.QueryAsync<MemberInfo>(
            """
            SELECT u.email, u.first_name AS name
            FROM users u
            WHERE u.id = ANY(@UserIds)
            """,
            new { UserIds = @event.MemberUserIds.ToArray() });

        foreach (var member in members)
        {
            var email = templateService.OrganizationDeactivated(member.Email, member.Name ?? "there", orgName);
            var result = await emailService.SendAsync(email, cancellationToken);

            if (result.IsFailure)
                throw new InvalidOperationException($"Failed to send organization.deactivated email to {member.Email}: {result.Error.Code}");
        }
    }

    private sealed record MemberInfo
    {
        public required string Email { get; init; }
        public string? Name { get; init; }
    }
}
```

- [ ] **Step 3: Build and run tests**

```bash
dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~OrganizationDeactivatedEventHandlerTests" -v minimal
```

Expected: BUILD SUCCESS, PASS (1 test)

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Organizations/EventHandlers/ \
        tests/Propentro.Application.Tests/Features/Admin/Organizations/
git commit -m "feat: add organization deactivated outbox event handler

Queries users by MemberUserIds from event payload (captured before
hard delete in DeactivateOrganizationHandler). Sends deactivation
notification email to each former member."
```

---

## Task 9: Full Build + Test Verification

Final verification that everything works together.

- [ ] **Step 1: Clean build**

```bash
dotnet clean Propentro.slnx && dotnet build Propentro.slnx
```

Expected: SUCCESS with zero warnings (TreatWarningsAsErrors is enabled)

- [ ] **Step 2: Run all tests**

```bash
dotnet test Propentro.slnx -v minimal
```

Expected: ALL PASS

- [ ] **Step 3: Verify outbox handler discovery**

```bash
grep -r "class.*EventHandler.*OutboxEventHandler" src/Propentro.Application/ --include="*.cs" | wc -l
```

Expected: 7 handler classes. All in `Propentro.Application` assembly, all non-abstract, all implement `IOutboxEventHandler` through the base class. `AddOutboxEventHandlers()` scans `typeof(IOutboxEventHandler).Assembly` (= Application assembly) and will discover all 8.

- [ ] **Step 4: Final commit (if any remaining changes)**

If there are any uncommitted fixes from the verification steps, commit them.

---

## Follow-Up Items (Out of Scope)

These items are documented for future work and should be tracked as Linear tickets:

1. **Org reactivation notification:** Add `OrganizationReactivatedEvent` + handler once deactivation is changed to soft-delete `organization_users` rows. Currently not implemented because hard-deleted memberships leave no members to notify on reactivation.
2. **Integration tests:** The spec calls for "integration tests for at least one end-to-end flow: command → outbox message persisted → verify outbox row exists." This requires Testcontainers and is a separate task.
3. **Email template improvements:** Current templates use inline HTML. Migrate to React Email or a template engine when design requirements are established.
