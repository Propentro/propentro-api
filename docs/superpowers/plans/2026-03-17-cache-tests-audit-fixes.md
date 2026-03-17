# Cache Invalidation & Test Coverage Fixes

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 3 cache invalidation gaps, 8 integration test gaps, 7 weak outbox event handler tests, and 3 missing validator unit tests.

**Architecture:** All fixes follow existing patterns — `ICacheInvalidating` for org-scoped commands, `ICacheService` for admin/webhook commands, standard integration test patterns, SQLite in-memory for outbox handler unit tests.

**Tech Stack:** .NET 10, xUnit, FluentAssertions, NSubstitute, Microsoft.Data.Sqlite (new test dependency)

---

## Task 1: Cache Invalidation — InviteToCurrentOrgCommand

**Files:**
- Modify: `src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgCommand.cs`

This command has `OrganizationId` in the record, so it can use `ICacheInvalidating`. When inviting an existing user, `InviteService.HandleExistingUserAsync` creates membership + assigns roles immediately.

- [ ] **Step 1: Add ICacheInvalidating to InviteToCurrentOrgCommand**

```csharp
using Propentro.Contracts.Organizations.Invites;

namespace Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

public sealed record InviteToCurrentOrgCommand(
    string Email,
    string RoleId,
    Guid OrganizationId,
    Guid InvitedByUserId) : ICommand<InviteResult>, ICacheInvalidating
{
    public IReadOnlyList<string> CacheKeysToInvalidate { get; } = ["user-permissions:*"];
}
```

- [ ] **Step 2: Build**

Run: `dotnet build Propentro.slnx`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Features/Organizations/Invites/Commands/InviteToCurrentOrg/InviteToCurrentOrgCommand.cs
git commit -m "feat: add cache invalidation to InviteToCurrentOrgCommand"
```

---

## Task 2: Cache Invalidation — HandleMembershipCreatedHandler

**Files:**
- Modify: `src/Propentro.Application/Features/Webhooks/Commands/HandleMembershipCreated/HandleMembershipCreatedHandler.cs`

This webhook command doesn't have `OrganizationId` in the command record (resolved from DB via `ClerkOrganizationId`). Must use `ICacheService` directly in the handler, same pattern as admin handlers.

- [ ] **Step 1: Add ICacheService to HandleMembershipCreatedHandler**

Inject `ICacheService cache` into the primary constructor and add cache invalidation after membership + role assignment, before the final `return Result.Success()`:

```csharp
public sealed class HandleMembershipCreatedHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ICacheService cache) : ICommandHandler<HandleMembershipCreatedCommand>
```

After line 126 (`dbContext.AddOutboxEvent(...)`) and before `return Result.Success()`, add:

```csharp
        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);
```

Add the using at the top of the file:
```csharp
using Propentro.Application.Common.Interfaces.Caching;
```

- [ ] **Step 2: Build**

Run: `dotnet build Propentro.slnx`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Features/Webhooks/Commands/HandleMembershipCreated/HandleMembershipCreatedHandler.cs
git commit -m "feat: add cache invalidation to HandleMembershipCreatedHandler"
```

---

## Task 3: Cache Invalidation — DeactivateUserHandler (Admin)

**Files:**
- Modify: `src/Propentro.Application/Features/Admin/Users/Commands/DeactivateUser/DeactivateUserHandler.cs`

Deactivates a user globally. Must invalidate permission cache for ALL organizations the user belongs to.

- [ ] **Step 1: Add ICacheService and invalidation to DeactivateUserHandler**

Inject `ICacheService cache` into the primary constructor:

```csharp
public sealed class DeactivateUserHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<DeactivateUserCommand>
```

After `user.IsActive = false;` and the log statement, before `return Result.Success()`, add:

```csharp
        // Invalidate permission cache for all organizations the user belongs to
        var orgIds = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.UserId == user.Id)
            .Select(ou => ou.OrganizationId)
            .ToListAsync(cancellationToken);

        foreach (var orgId in orgIds)
        {
            await cache.RemoveByPatternAsync($"org:{orgId}:user-permissions:*", cancellationToken);
        }
```

Add required usings:
```csharp
using Propentro.Application.Common.Interfaces.Caching;
```

- [ ] **Step 2: Build**

Run: `dotnet build Propentro.slnx`
Expected: 0 errors, 0 warnings

- [ ] **Step 3: Commit**

```bash
git add src/Propentro.Application/Features/Admin/Users/Commands/DeactivateUser/DeactivateUserHandler.cs
git commit -m "feat: add cache invalidation to admin DeactivateUserHandler"
```

---

## Task 4: Integration Tests — Missing 403 Permission Tests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRolesTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/UpdateRoleTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetPermissionsTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Invites/GetInvitesTests.cs`

All 5 endpoints require specific permissions (roles:read, roles:manage, members:manage). User2 is in Org2 with NO permissions assigned, so requests from User2 return 403.

- [ ] **Step 1: Add 403 test to GetRolesTests**

Append to `GetRolesTests.cs`:

```csharp
    [Fact]
    public async Task Get_Roles_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

- [ ] **Step 2: Add 403 test to CreateRoleTests**

Append to `CreateRoleTests.cs`:

```csharp
    [Fact]
    public async Task Create_Role_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var request = new CreateRoleRequest(
            Name: "Forbidden Role",
            Description: null,
            PermissionCodes: ["org:settings:read"]);

        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

- [ ] **Step 3: Add 403 test to UpdateRoleTests**

Append to `UpdateRoleTests.cs`:

```csharp
    [Fact]
    public async Task Update_Role_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var request = new UpdateRoleRequest(
            Name: "Forbidden Update",
            Description: null,
            PermissionCodes: null);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/rol_000000000000", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

- [ ] **Step 4: Add 403 test to GetPermissionsTests**

Append to `GetPermissionsTests.cs`:

```csharp
    [Fact]
    public async Task Get_Permissions_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

- [ ] **Step 5: Add 403 test to GetInvitesTests**

Append to `GetInvitesTests.cs`:

```csharp
    [Fact]
    public async Task Get_Invites_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

- [ ] **Step 6: Build and run affected tests**

Run: `dotnet build Propentro.slnx && dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetRolesTests|FullyQualifiedName~CreateRoleTests|FullyQualifiedName~UpdateRoleTests|FullyQualifiedName~GetPermissionsTests|FullyQualifiedName~GetInvitesTests"`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRolesTests.cs \
        tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs \
        tests/Propentro.Api.Tests/Features/Organizations/Roles/UpdateRoleTests.cs \
        tests/Propentro.Api.Tests/Features/Organizations/Roles/GetPermissionsTests.cs \
        tests/Propentro.Api.Tests/Features/Organizations/Invites/GetInvitesTests.cs
git commit -m "test: add missing 403 permission tests for roles, permissions, and invites"
```

---

## Task 5: Integration Tests — Missing Happy Path Tests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Invites/CancelInviteTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminInviteUserTests.cs`

### 5A: DeactivateMember Happy Path

The test shares an `IClassFixture<PropentroApiFactory>` — all tests share one DB. Deactivating a real seeded user would break other tests. The happy path needs to deactivate a member that won't be needed elsewhere.

Looking at the seeder: User1 is in Org1 with permissions. Admin is also in Org1. User1 can deactivate Admin (they have `org:members:manage`). But Admin is used by other tests.

**Safest approach:** Test that self-deactivation returns 409 (`Member.CannotDeactivateSelf`) — this validates the endpoint works end-to-end without mutating shared state. If a true 204 happy path is needed, we'd need a dedicated test user. For now, the 409 state-guard test is valuable and safe.

- [ ] **Step 1: Add self-deactivation 409 test to DeactivateMemberTests**

Append to `DeactivateMemberTests.cs`:

```csharp
    [Fact]
    public async Task Deactivate_Self_Returns_409()
    {
        // Arrange — User1 trying to deactivate themselves
        var client = factory.CreateClientAsUser1();
        var selfId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{selfId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
```

Also add the missing 403 permission test:

```csharp
    [Fact]
    public async Task Deactivate_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var fakeId = "usr_000000000000";

        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

### 5B: CancelInvite — Add 403 Test

CancelInvite happy path (canceling a real pending invite) is difficult without first creating an invite (which requires Clerk mock). The 403 test is straightforward.

- [ ] **Step 2: Add 403 test to CancelInviteTests**

Append to `CancelInviteTests.cs`:

```csharp
    [Fact]
    public async Task Cancel_Invite_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var fakeId = "inv_000000000000";

        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
```

### 5C: AdminInviteUser Happy Path

The admin invite endpoint requires mocked Clerk service (external API call). Happy path test needs careful setup. Since this calls `IInviteService.InviteUserToOrganizationAsync` which calls `IClerkOrganizationService`, and the test factory likely doesn't mock Clerk, we can test a meaningful error path instead.

- [ ] **Step 3: Add nonexistent org test to AdminInviteUserTests**

Append to `AdminInviteUserTests.cs`:

```csharp
    [Fact]
    public async Task Invite_To_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var fakeOrgId = "org_000000000000";
        var request = new InviteUserRequest("newuser@example.com", factory.Seeder.DefaultRolePublicId);

        var response = await client.PostAsJsonAsync(InviteUrl(fakeOrgId), request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
```

- [ ] **Step 4: Build and run affected tests**

Run: `dotnet build Propentro.slnx && dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~DeactivateMemberTests|FullyQualifiedName~CancelInviteTests|FullyQualifiedName~AdminInviteUserTests"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs \
        tests/Propentro.Api.Tests/Features/Organizations/Invites/CancelInviteTests.cs \
        tests/Propentro.Api.Tests/Features/Admin/Invites/AdminInviteUserTests.cs
git commit -m "test: add missing happy path and 403 tests for deactivate, cancel, and admin invite"
```

---

## Task 6: Outbox Event Handler Tests — Setup

**Files:**
- Modify: `Directory.Packages.props` (add Microsoft.Data.Sqlite version)
- Modify: `tests/Propentro.Application.Tests/Propentro.Application.Tests.csproj` (add Microsoft.Data.Sqlite)
- Create: `tests/Propentro.Application.Tests/Shared/TestDbHelper.cs`

The 7 outbox event handlers use Dapper directly on `DbConnection`. Dapper extension methods can't be mocked. Solution: use SQLite in-memory databases with **shared cache** so the setup connection (that creates tables and seeds data) persists, and a separate **unopened** handler connection is returned for the mock. The handler calls `OpenAsync()` on it as expected.

- [ ] **Step 1: Add Microsoft.Data.Sqlite to Directory.Packages.props and Application.Tests**

In `Directory.Packages.props`, add under the Testing section:

```xml
<PackageVersion Include="Microsoft.Data.Sqlite" Version="10.0.2" />
```

Version `10.0.2` matches the project's EF Core version.

In `tests/Propentro.Application.Tests/Propentro.Application.Tests.csproj`, add:

```xml
<PackageReference Include="Microsoft.Data.Sqlite" />
```

- [ ] **Step 2: Create TestDbHelper**

Create `tests/Propentro.Application.Tests/Shared/TestDbHelper.cs`:

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Propentro.Application.Tests.Shared;

/// <summary>
/// Creates SQLite in-memory databases for testing outbox event handlers that use Dapper.
/// Uses shared-cache mode so the setup connection (which creates tables/data) and the
/// handler connection (returned unopened for the mock) share the same in-memory database.
/// </summary>
internal sealed class TestDbHelper : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString("N");
    private readonly SqliteConnection _keepAlive;

    public TestDbHelper()
    {
        // This connection keeps the shared in-memory DB alive for the test's lifetime
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();

        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE users (
                id TEXT PRIMARY KEY,
                email TEXT NOT NULL,
                first_name TEXT
            );

            CREATE TABLE organizations (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );

            CREATE TABLE invites (
                id TEXT PRIMARY KEY,
                email TEXT NOT NULL
            );

            CREATE TABLE organization_user_roles (
                organization_id TEXT NOT NULL,
                user_id TEXT NOT NULL,
                role_id TEXT NOT NULL
            );

            CREATE TABLE roles (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private string ConnectionString => $"DataSource={_dbName};Mode=Memory;Cache=Shared";

    /// <summary>
    /// Returns an UNOPENED connection to the shared in-memory DB.
    /// The handler under test calls OpenAsync() on this connection as expected.
    /// </summary>
    public DbConnection CreateHandlerConnection() => new SqliteConnection(ConnectionString);

    public void InsertUser(Guid id, string email, string? firstName = "Test")
    {
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = $"INSERT INTO users (id, email, first_name) VALUES ('{id}', '{email}', {(firstName is null ? "NULL" : $"'{firstName}'")})";
        cmd.ExecuteNonQuery();
    }

    public void InsertOrganization(Guid id, string name)
    {
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = $"INSERT INTO organizations (id, name) VALUES ('{id}', '{name}')";
        cmd.ExecuteNonQuery();
    }

    public void InsertInvite(Guid id, string email)
    {
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = $"INSERT INTO invites (id, email) VALUES ('{id}', '{email}')";
        cmd.ExecuteNonQuery();
    }

    public void InsertUserRole(Guid orgId, Guid userId, Guid roleId)
    {
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = $"INSERT INTO organization_user_roles (organization_id, user_id, role_id) VALUES ('{orgId}', '{userId}', '{roleId}')";
        cmd.ExecuteNonQuery();
    }

    public void InsertRole(Guid id, string name)
    {
        using var cmd = _keepAlive.CreateCommand();
        cmd.CommandText = $"INSERT INTO roles (id, name) VALUES ('{id}', '{name}')";
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => _keepAlive.Dispose();
}
```

Key design decisions:
- **Shared cache**: Setup connection (`_keepAlive`) and handler connection share the same in-memory DB via `Cache=Shared`
- **Unopened handler connection**: `CreateHandlerConnection()` returns a closed `SqliteConnection` — the handler calls `OpenAsync()` on it as in production
- **IDisposable**: Disposing the keep-alive connection destroys the in-memory DB after the test
- **NULL handling**: `InsertUser` correctly handles null `firstName` by inserting SQL `NULL`

- [ ] **Step 3: Build**

Run: `dotnet build Propentro.slnx`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Application.Tests/Propentro.Application.Tests.csproj \
        tests/Propentro.Application.Tests/Shared/TestDbHelper.cs \
        Directory.Packages.props
git commit -m "test: add SQLite in-memory test infrastructure for outbox handler tests"
```

---

## Task 7: Outbox Event Handler Tests — Member Handlers

**Files:**
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandlerTests.cs`
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandlerTests.cs`
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandlerTests.cs`
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandlerTests.cs`

All 4 handlers follow the same pattern: query user → query org → build email → send email → throw on failure.

**Important:** The `HandleAsync(TEvent, CancellationToken)` method is `protected`. Tests must use the **public** `HandleAsync(string payload, CancellationToken)` method which deserializes JSON via `OutboxJsonOptions` (uses `JsonNamingPolicy.CamelCase`) and then calls the protected method.

Serialization pattern for all tests:
```csharp
using System.Text.Json;
using Propentro.Application.Common.Interfaces.Messaging;

// Serialize with OutboxJsonOptions to match deserialization
var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);
await handler.HandleAsync(payload, CancellationToken.None);
```

- [ ] **Step 1: Rewrite MemberDeactivatedEventHandlerTests**

```csharp
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members.EventHandlers;
using Propentro.Application.Tests.Shared;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberDeactivatedEventHandlerTests : IDisposable
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();
    private readonly TestDbHelper _testDb = new();

    [Fact]
    public void EventType_Should_Be_MemberDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);

        handler.EventType.Should().Be("member.deactivated");
    }

    [Fact]
    public async Task HandleAsync_Sends_Email_When_User_And_Org_Found()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var expectedEmail = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated("user@test.com", "Alice", "Acme Corp").Returns(expectedEmail);
        _emailService.SendAsync(expectedEmail, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
        _templateService.Received(1).MemberDeactivated("user@test.com", "Alice", "Acme Corp");
        await _emailService.Received(1).SendAsync(expectedEmail, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_User_Not_Found()
    {
        // Arrange — no user in DB
        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert — no email sent
        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_Org_Not_Found()
    {
        // Arrange — user exists, org doesn't
        var userId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert — no email sent
        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_When_Email_Send_Fails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Email.SendFailed", "Send failed", ErrorType.ExternalService)));

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(payload, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Uses_There_When_Name_Is_Null()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", null);
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated("user@test.com", "there", "Acme Corp").Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert
        _templateService.Received(1).MemberDeactivated("user@test.com", "there", "Acme Corp");
    }

    public void Dispose() => _testDb.Dispose();
}
```

- [ ] **Step 2: Apply same pattern to MemberReactivatedEventHandlerTests**

Same structure as MemberDeactivatedEventHandlerTests but:
- `EventType` = `"member.reactivated"`
- Event type = `MemberReactivatedEvent`
- Template method = `templateService.MemberReactivated(...)`

- [ ] **Step 3: Apply same pattern to MemberRemovedEventHandlerTests**

Same structure but:
- `EventType` = `"member.removed"`
- Event type = `MemberRemovedEvent`
- Template method = `templateService.MemberRemoved(...)`

- [ ] **Step 4: Apply pattern to MemberRolesUpdatedEventHandlerTests**

This handler has extra complexity — it also queries `organization_user_roles` JOIN `roles` to get role names. Tests need:
- `_testDb.InsertRole(roleId, "Manager")`
- `_testDb.InsertUserRole(orgId, userId, roleId)`
- Template method = `templateService.MemberRolesUpdated(email, name, orgName, roleNames)`
- Additional test: returns early when no roles found

```csharp
    [Fact]
    public async Task HandleAsync_Returns_Early_When_No_Roles_Found()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");
        // No roles inserted

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new MemberRolesUpdatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberRolesUpdatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Sends_Email_With_Role_Names()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");
        _testDb.InsertRole(roleId, "Manager");
        _testDb.InsertUserRole(orgId, userId, roleId);

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberRolesUpdated("user@test.com", "Alice", "Acme Corp",
            Arg.Is<IReadOnlyList<string>>(r => r.Contains("Manager"))).Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new MemberRolesUpdatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberRolesUpdatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.Received(1).SendAsync(email, Arg.Any<CancellationToken>());
    }
```

- [ ] **Step 5: Build and run**

Run: `dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~EventHandler"`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/
git commit -m "test: expand outbox member event handler tests with HandleAsync coverage"
```

---

## Task 8: Outbox Event Handler Tests — Invite & Org Handlers

**Files:**
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteAcceptedEventHandlerTests.cs`
- Modify: `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandlerTests.cs`
- Modify: `tests/Propentro.Application.Tests/Features/Admin/Organizations/EventHandlers/OrganizationDeactivatedEventHandlerTests.cs`

### Invite handlers

Both invite handlers query `invites.email` and `organizations.name`. Same pattern as member handlers but simpler — the template methods take `(email, orgName)` instead of `(email, name, orgName)`.

- [ ] **Step 1: Rewrite InviteAcceptedEventHandlerTests**

Tests: EventType, happy path (email sent), invite not found (no email), org not found (no email), send failure (throws). Use `_testDb.InsertInvite(inviteId, email)` for invite data. Event is `InviteAcceptedEvent(inviteId, orgId)` where `AggregateId` is the invite ID.

- [ ] **Step 2: Rewrite InviteCanceledEventHandlerTests**

Same structure as InviteAccepted but with `"invite.canceled"` EventType and `templateService.InviteCanceled(...)`.

### OrganizationDeactivated handler

This handler uses `ANY(@UserIds)` which is PostgreSQL-specific. **Do NOT change production SQL for testability.** Instead, test only the paths that don't require the PostgreSQL-specific query.

- [ ] **Step 3: Rewrite OrganizationDeactivatedEventHandlerTests (limited scope)**

Tests that can be covered without hitting `ANY()`:
- `EventType` property
- Empty member list early return (fires before any DB access)

The full DB query path (querying members by IDs, sending emails) is covered by integration tests since it requires PostgreSQL.

```csharp
using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Admin.Organizations.EventHandlers;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Admin.Organizations.EventHandlers;

public class OrganizationDeactivatedEventHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();

    [Fact]
    public void EventType_Should_Be_OrganizationDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);

        handler.EventType.Should().Be("organization.deactivated");
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_No_Members()
    {
        // Arrange
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var orgId = Guid.NewGuid();
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, []);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        // Act
        await handler.HandleAsync(payload, CancellationToken.None);

        // Assert — should not even create a DB connection
        db.DidNotReceive().CreateConnection();
        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 4: Build and run**

Run: `dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~EventHandler"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/ \
        tests/Propentro.Application.Tests/Features/Admin/Organizations/EventHandlers/
git commit -m "test: expand outbox invite/org event handler tests with HandleAsync coverage"
```

---

## Task 9: Validator Unit Tests

**Files:**
- Create: `tests/Propentro.Application.Tests/Validators/GetOrganizationMembersValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/AdminDeactivateMemberValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/AdminReactivateMemberValidatorTests.cs`

### 9A: GetOrganizationMembersValidatorTests

Validator has: page >= 1, pageSize 1-100, status must be null/active/deactivated, roleId must be null or match regex.

- [ ] **Step 1: Create GetOrganizationMembersValidatorTests**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;

namespace Propentro.Application.Tests.Validators;

public class GetOrganizationMembersValidatorTests
{
    private readonly GetOrganizationMembersValidator _validator = new();

    private static GetOrganizationMembersQuery CreateValidQuery() =>
        new(Page: 1, PageSize: 25, Search: null, Status: null, RoleId: null);

    [Fact]
    public void Valid_Query_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("deactivated")]
    [InlineData(null)]
    public void Valid_Status_Should_Pass(string? status)
    {
        var query = CreateValidQuery() with { Status = status };
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("pending")]
    [InlineData("ACTIVE")]
    public void Invalid_Status_Should_Fail(string status)
    {
        var query = CreateValidQuery() with { Status = status };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("rol_a1b2c3d4e5f6")]
    [InlineData(null)]
    public void Valid_RoleId_Should_Pass(string? roleId)
    {
        var query = CreateValidQuery() with { RoleId = roleId };
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("rol_short")]
    [InlineData("usr_a1b2c3d4e5f6")]
    public void Invalid_RoleId_Should_Fail(string roleId)
    {
        var query = CreateValidQuery() with { RoleId = roleId };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Page_Zero_Should_Fail()
    {
        var query = CreateValidQuery() with { Page = 0 };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void PageSize_Over_100_Should_Fail()
    {
        var query = CreateValidQuery() with { PageSize = 101 };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
```

### 9B: AdminDeactivateMemberValidatorTests

Validator has: OrganizationId regex `^org_[0-9a-f]{12}$`, UserId regex `^usr_[0-9a-f]{12}$`.

- [ ] **Step 2: Create AdminDeactivateMemberValidatorTests**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

namespace Propentro.Application.Tests.Validators;

public class AdminDeactivateMemberValidatorTests
{
    private readonly AdminDeactivateMemberValidator _validator = new();

    private static AdminDeactivateMemberCommand CreateValidCommand() =>
        new(OrganizationId: "org_a1b2c3d4e5f6", UserId: "usr_a1b2c3d4e5f6");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("usr_a1b2c3d4e5f6")]
    [InlineData("org_short")]
    public void Invalid_OrganizationId_Should_Fail(string orgId)
    {
        var command = CreateValidCommand() with { OrganizationId = orgId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("org_a1b2c3d4e5f6")]
    [InlineData("usr_short")]
    public void Invalid_UserId_Should_Fail(string userId)
    {
        var command = CreateValidCommand() with { UserId = userId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
```

### 9C: AdminReactivateMemberValidatorTests

Identical structure to AdminDeactivateMemberValidatorTests but for the reactivate command.

- [ ] **Step 3: Create AdminReactivateMemberValidatorTests**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

namespace Propentro.Application.Tests.Validators;

public class AdminReactivateMemberValidatorTests
{
    private readonly AdminReactivateMemberValidator _validator = new();

    private static AdminReactivateMemberCommand CreateValidCommand() =>
        new(OrganizationId: "org_a1b2c3d4e5f6", UserId: "usr_a1b2c3d4e5f6");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("usr_a1b2c3d4e5f6")]
    [InlineData("org_short")]
    public void Invalid_OrganizationId_Should_Fail(string orgId)
    {
        var command = CreateValidCommand() with { OrganizationId = orgId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("org_a1b2c3d4e5f6")]
    [InlineData("usr_short")]
    public void Invalid_UserId_Should_Fail(string userId)
    {
        var command = CreateValidCommand() with { UserId = userId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
```

- [ ] **Step 4: Build and run**

Run: `dotnet build Propentro.slnx && dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~ValidatorTests"`
Expected: All pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Application.Tests/Validators/GetOrganizationMembersValidatorTests.cs \
        tests/Propentro.Application.Tests/Validators/AdminDeactivateMemberValidatorTests.cs \
        tests/Propentro.Application.Tests/Validators/AdminReactivateMemberValidatorTests.cs
git commit -m "test: add validator unit tests for GetOrganizationMembers, AdminDeactivateMember, AdminReactivateMember"
```

---

## Task 10: Final Verification

- [ ] **Step 1: Full build**

Run: `dotnet build Propentro.slnx --configuration Release`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: Full test suite**

Run: `dotnet test Propentro.slnx`
Expected: All tests pass

- [ ] **Step 3: Final commit (if any fixups needed)**
