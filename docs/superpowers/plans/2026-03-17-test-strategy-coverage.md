# Test Strategy & Coverage Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring integration test coverage from 26% to 100% (all 39 endpoints), add targeted unit tests for risky areas, and codify the test strategy in documentation.

**Architecture:** Testing Trophy approach — integration tests are the backbone (full HTTP → DB → response), unit tests only for risky logic (state guards, event handlers, complex validators). All tests share `PropentroApiFactory` (Testcontainers + PostgreSQL 17) with `DatabaseSeeder` providing 2 orgs, 3 users, roles, and permissions.

**Tech Stack:** xUnit, FluentAssertions 7.x, NSubstitute, FluentValidation.TestHelper, Testcontainers (PostgreSQL 17-alpine)

---

## File Map

### Phase 1: Documentation
- Modify: `CLAUDE.md` — update "When to Write Which Test" table, add "Risky Areas" section
- Create: `docs/testing-strategy.md` — full test philosophy and decision tree

### Phase 2: Missing Integration Tests (29 new files)

**Org-Scoped (8 new files):**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/GetMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/ReactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/UpdateMemberRolesTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRolesTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRoleTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/UpdateRoleTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Invites/CancelInviteTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Invites/GetInvitesTests.cs`

**Org-Scoped — Special (1 new file):**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetPermissionsTests.cs`

**Webhook (1 new file):**
- Create: `tests/Propentro.Api.Tests/Features/Webhooks/ClerkWebhookTests.cs`

**Admin Organizations (5 new files):**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/GetOrganizationsTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/GetOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/UpdateOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/ReactivateOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeleteOrganizationTests.cs`

**Admin Users (5 new files):**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/GetUsersTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/GetUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/DeactivateUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/ReactivateUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/DeleteUserTests.cs`

**Admin Me (1 new file):**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Me/AdminGetProfileTests.cs`

**Admin Members (4 new files):**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminGetMembersTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminDeactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminReactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminRemoveMemberTests.cs`

**Admin Invites (4 new files):**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminInviteUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminGetOrgInvitesTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminCancelInviteTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminGetInvitesTests.cs`

### Phase 3: Unit Tests (9 new files + entity verification)

**Entity Verification (0 new files — see Task 3.3):**
- Verify: `src/Propentro.Domain/Entities/Identity/Organization.cs` — no state transition methods
- Verify: `src/Propentro.Domain/Entities/Identity/User.cs` — no state transition methods
- Verify: `src/Propentro.Domain/Entities/Identity/OrganizationUser.cs` — no state transition methods
- Modify: `docs/testing-strategy.md` — document conscious exclusion

**Event Handlers (4 new files):**
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandlerTests.cs`

**Validators (5 new files):**
- Create: `tests/Propentro.Application.Tests/Validators/InviteToCurrentOrgValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/AdminInviteUserValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/UpdateMemberRolesValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/CreateOrganizationValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/UpdateOrganizationValidatorTests.cs`

### Phase 4: Audit Existing Tests (10 files modified)
- Modify: `tests/Propentro.Api.Tests/Features/Admin/Organizations/CreateOrganizationTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeactivateOrganizationTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Me/GetCurrentUserTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Invites/InviteUserTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Members/GetOrganizationMembersTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/DeleteRoleTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Settings/GetOrganizationSettingsTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Settings/UpdateOrganizationSettingsTests.cs`

---

## Conventions & Reference

Before writing any test, review these reference files:

- **Integration test pattern**: `tests/Propentro.Api.Tests/Features/Admin/Organizations/CreateOrganizationTests.cs`
- **Factory**: `tests/Propentro.Api.Tests/Shared/PropentroApiFactory.cs`
- **Seeder**: `tests/Propentro.Api.Tests/Shared/DatabaseSeeder.cs` — provides `Organization1PublicId`, `Organization2PublicId`, `User1PublicId`, `User2PublicId`, `AdminUserPublicId`, `DefaultRolePublicId`
- **Client helpers**: `tests/Propentro.Api.Tests/Shared/HttpClientExtensions.cs` — `CreateClientAsUser1()` (org1, all permissions), `CreateClientAsUser2()` (org2), `CreateClientAsAdmin()` (system admin), `CreateAnonymousClient()`
- **Event handler test pattern**: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberDeactivatedEventHandlerTests.cs`
- **Validator test pattern**: `tests/Propentro.Application.Tests/Validators/CreateRoleValidatorTests.cs`

**Key conventions:**
- Class: `public sealed class XxxTests(PropentroApiFactory factory) : IClassFixture<PropentroApiFactory>`
- Fake IDs: `usr_000000000000`, `org_000000000000`, `rol_000000000000`, `inv_000000000000`
- Cross-tenant: User1 (org1) tries to access org2 resource → expect 404 (not 403, because multi-tenancy filter hides it)
- 409 state guards: deactivate already-inactive entity, reactivate already-active entity
- `IClassFixture` means shared server — never do destructive operations that break other tests in the same class

---

## Phase 1: Documentation

### Task 1.1: Update CLAUDE.md Testing Section

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Replace "When to Write Which Test" table**

Replace the existing table in the `## Testing` section → `### When to Write Which Test` with:

```markdown
### When to Write Which Test

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
```

- [ ] **Step 2: Add Risky Areas section after the table**

Add immediately after the "When to Write Which Test" table:

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

For the full testing philosophy, decision tree, and coverage matrix, see `docs/testing-strategy.md`.
```

- [ ] **Step 3: Build to verify no syntax issues**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: update CLAUDE.md testing rules with risky areas and coverage requirements"
```

### Task 1.2: Create Testing Strategy Document

**Files:**
- Create: `docs/testing-strategy.md`

- [ ] **Step 1: Write docs/testing-strategy.md**

The document should contain these sections in order:

1. **Philosophy** — Testing Trophy (Kent C. Dodds), pragmatic approach, why not 100% unit coverage. Integration tests are the backbone (highest bug-per-test ROI). Unit tests reserved for risky logic.

2. **Test Layers** — Table describing each test project:
   - `Propentro.Api.Tests` — Integration: full HTTP → DB → response
   - `Propentro.Application.Tests` — Unit: validators, outbox event handlers
   - `Propentro.Domain.Tests` — Unit: entity state transitions, value objects
   - `Propentro.Infrastructure.Tests` — Unit: email templates, external service logic
   - `Propentro.Architecture.Tests` — Structural: layer dependencies, CQRS conventions

3. **Integration Test Case Matrix** — Table of which test cases apply to which endpoint type:
   - Happy path: Always
   - 401: All endpoints (except webhook)
   - 403: All endpoints requiring specific permissions
   - 400: Endpoints with a validator
   - 404: GET/PATCH/DELETE on single resources
   - Cross-tenant 404: Org-scoped GET/PATCH/DELETE
   - 409: State transition endpoints
   - Soft delete filtering: Listing endpoints
   - X-Confirm-Delete: Hard delete endpoints

4. **Risky Area Definition** — When unit tests are required: state transitions, hard deletes, permission logic, money, conditional business logic, external service integration. When NOT required: simple validators, CRUD, Dapper reads, EF configs.

5. **Decision Tree** — "I changed X, which test do I write?" flowchart in text form.

6. **Conscious Exclusions** — What we don't test and why: simple validators, CRUD handlers, Dapper queries, EF configurations.

- [ ] **Step 2: Commit**

```bash
git add docs/testing-strategy.md
git commit -m "docs: add testing strategy document with coverage matrix and decision tree"
```

---

## Phase 2: Missing Integration Tests

> **Parallelization note:** Tasks 2.1–2.10 are fully independent. Each creates new test files that don't interact. They can be run by parallel subagents.

### Task 2.1: Org Members — GetMember, ReactivateMember, UpdateMemberRoles

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/GetMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/ReactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/UpdateMemberRolesTests.cs`

**Reference:** Existing `DeactivateMemberTests.cs` and `GetOrganizationMembersTests.cs` in same directory.

- [ ] **Step 1: Write GetMemberTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class GetMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Get_Member_Returns_200()
    {
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Member_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"{BaseUrl}/usr_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync($"{BaseUrl}/usr_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Member_Cross_Tenant_Returns_404()
    {
        // User1 is in org1, User2 is in org2 — User1 cannot see User2
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{user2Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Write ReactivateMemberTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class ReactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Reactivate_Already_Active_Member_Returns_409()
    {
        // User1 is already active in org1
        var client = factory.CreateClientAsUser1();
        var adminId = factory.Seeder.AdminUserPublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{adminId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reactivate_Cross_Tenant_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{user2Id}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 3: Write UpdateMemberRolesTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class UpdateMemberRolesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Update_Roles_Returns_204()
    {
        var client = factory.CreateClientAsUser1();
        var adminId = factory.Seeder.AdminUserPublicId;
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateMemberRolesRequest([roleId]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{adminId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Roles_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new UpdateMemberRolesRequest(["rol_000000000000"]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/usr_000000000000/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Roles_With_Empty_Array_Returns_400()
    {
        var client = factory.CreateClientAsUser1();
        var adminId = factory.Seeder.AdminUserPublicId;
        var request = new UpdateMemberRolesRequest([]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{adminId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Roles_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateMemberRolesRequest([roleId]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/usr_000000000000/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Roles_Cross_Tenant_Returns_404()
    {
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateMemberRolesRequest([roleId]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{user2Id}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetMemberTests|FullyQualifiedName~ReactivateMemberTests|FullyQualifiedName~UpdateMemberRolesTests" -v minimal`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Members/GetMemberTests.cs tests/Propentro.Api.Tests/Features/Organizations/Members/ReactivateMemberTests.cs tests/Propentro.Api.Tests/Features/Organizations/Members/UpdateMemberRolesTests.cs
git commit -m "test: add integration tests for GetMember, ReactivateMember, UpdateMemberRoles"
```

### Task 2.2: Org Roles — GetRoles, GetRole, UpdateRole, GetPermissions

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRolesTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRoleTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/UpdateRoleTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/GetPermissionsTests.cs`

**Reference:** Existing `CreateRoleTests.cs` and `DeleteRoleTests.cs` in same directory. Route base: `/v1/organizations/roles` and `/v1/organizations/permissions`.

- [ ] **Step 1: Write GetRolesTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class GetRolesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/roles";

    [Fact]
    public async Task Get_Roles_Returns_200()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Roles_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Write GetRoleTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class GetRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/roles";

    [Fact]
    public async Task Get_Role_Returns_200()
    {
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;

        var response = await client.GetAsync($"{BaseUrl}/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Role_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"{BaseUrl}/rol_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_Role_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync($"{BaseUrl}/rol_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Role_Cross_Tenant_Returns_404()
    {
        // User2 is in org2, DefaultRole is in org1
        var client = factory.CreateClientAsUser2();
        var roleId = factory.Seeder.DefaultRolePublicId;

        var response = await client.GetAsync($"{BaseUrl}/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

Note: `CreateClientAsUser2()` authenticates as User2 in org2. The seeder's DefaultRole is in org1, so org2 cannot see it — this is the cross-tenant test.

- [ ] **Step 3: Write UpdateRoleTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class UpdateRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/roles";

    [Fact]
    public async Task Update_Role_Returns_204()
    {
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateRoleRequest("Updated Role", "Updated description", ["properties.read"]);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{roleId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Role_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new UpdateRoleRequest("Test", null, null);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/rol_000000000000", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Nonexistent_Role_Returns_404()
    {
        var client = factory.CreateClientAsUser1();
        var request = new UpdateRoleRequest("Test", null, null);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/rol_000000000000", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Role_With_Empty_Name_Returns_400()
    {
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateRoleRequest("", null, null);

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{roleId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 4: Write GetPermissionsTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class GetPermissionsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/permissions";

    [Fact]
    public async Task Get_Permissions_Returns_200()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Permissions_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetRolesTests|FullyQualifiedName~GetRoleTests|FullyQualifiedName~UpdateRoleTests|FullyQualifiedName~GetPermissionsTests" -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRolesTests.cs tests/Propentro.Api.Tests/Features/Organizations/Roles/GetRoleTests.cs tests/Propentro.Api.Tests/Features/Organizations/Roles/UpdateRoleTests.cs tests/Propentro.Api.Tests/Features/Organizations/Roles/GetPermissionsTests.cs
git commit -m "test: add integration tests for GetRoles, GetRole, UpdateRole, GetPermissions"
```

### Task 2.3: Org Invites — CancelInvite, GetInvites

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Invites/CancelInviteTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Invites/GetInvitesTests.cs`

**Reference:** Existing `InviteUserTests.cs` in same directory. Route base: `/v1/organizations/invites`.

- [ ] **Step 1: Write CancelInviteTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Invites;

public sealed class CancelInviteTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/invites";

    [Fact]
    public async Task Cancel_Nonexistent_Invite_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.PatchAsync($"{BaseUrl}/inv_000000000000/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_Invite_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/inv_000000000000/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

Note: Testing happy path (cancel a pending invite) and 409 (cancel non-pending) requires creating an invite first. Since `IClassFixture` shares state, these tests should create invites via the invite endpoint in Arrange. However, the `IClerkOrganizationService` is mocked, so the full invite flow may not complete. These minimal tests cover 401 and 404. If the invite creation succeeds through the mocked service, add a happy path test that first creates an invite, then cancels it.

- [ ] **Step 2: Write GetInvitesTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Invites;

public sealed class GetInvitesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/invites";

    [Fact]
    public async Task Get_Invites_Returns_200()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Invites_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~CancelInviteTests|FullyQualifiedName~GetInvitesTests" -v minimal`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Invites/CancelInviteTests.cs tests/Propentro.Api.Tests/Features/Organizations/Invites/GetInvitesTests.cs
git commit -m "test: add integration tests for CancelInvite, GetInvites (org-scoped)"
```

### Task 2.4: Clerk Webhook Tests

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Webhooks/ClerkWebhookTests.cs`

**Reference:** `src/Propentro.Api/Endpoints/WebhookEndpoints.cs`. The webhook endpoint is AllowAnonymous but verifies Svix signature headers. In test environment, `ClerkWebhookVerifier` may not be registered (no webhook secret configured), so signature verification is skipped.

- [ ] **Step 1: Write ClerkWebhookTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Webhooks;

public sealed class ClerkWebhookTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string WebhookUrl = "/v1/webhooks/clerk";

    [Fact]
    public async Task Webhook_Without_SvixId_Returns_400()
    {
        var client = factory.CreateAnonymousClient();
        var payload = JsonSerializer.Serialize(new
        {
            type = "organizationMembership.created",
            data = new { }
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(WebhookUrl, content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_With_Unhandled_Event_Type_Returns_200()
    {
        var client = factory.CreateAnonymousClient();
        var payload = JsonSerializer.Serialize(new
        {
            type = "user.updated",
            data = new { }
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);
        request.Content = content;
        request.Headers.Add("svix-id", $"msg_{Guid.NewGuid():N}");
        request.Headers.Add("svix-timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        request.Headers.Add("svix-signature", "v1,dummy");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_Duplicate_SvixId_Returns_200_Idempotent()
    {
        var client = factory.CreateAnonymousClient();
        var svixId = $"msg_{Guid.NewGuid():N}";
        var payload = JsonSerializer.Serialize(new
        {
            type = "user.updated",
            data = new { }
        });

        HttpRequestMessage CreateRequest()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, WebhookUrl);
            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            req.Headers.Add("svix-id", svixId);
            req.Headers.Add("svix-timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            req.Headers.Add("svix-signature", "v1,dummy");
            return req;
        }

        // First call
        using var request1 = CreateRequest();
        var response1 = await client.SendAsync(request1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Duplicate call — should also return 200 (idempotent skip)
        using var request2 = CreateRequest();
        var response2 = await client.SendAsync(request2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~ClerkWebhookTests" -v minimal`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Webhooks/ClerkWebhookTests.cs
git commit -m "test: add integration tests for Clerk webhook endpoint"
```

### Task 2.5: Admin Organizations — GetOrganizations, GetOrganization, UpdateOrganization, ReactivateOrganization, DeleteOrganization

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/GetOrganizationsTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/GetOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/UpdateOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/ReactivateOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeleteOrganizationTests.cs`

**Reference:** Existing `CreateOrganizationTests.cs` and `DeactivateOrganizationTests.cs`. All admin endpoints require system admin auth (`CreateClientAsAdmin()`). Regular users get 403.

- [ ] **Step 1: Write GetOrganizationsTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class GetOrganizationsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Get_Organizations_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Organizations_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Organizations_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Write GetOrganizationTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class GetOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Get_Organization_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{orgId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"{BaseUrl}/org_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync($"{BaseUrl}/org_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 3: Write UpdateOrganizationTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class UpdateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Update_Organization_As_Admin_Returns_204()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization2PublicId;
        var request = new UpdateOrganizationRequest(
            "Updated Org Name", null, null, null, "USD", "America/New_York");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new UpdateOrganizationRequest(
            "Hacked", null, null, null, "EUR", "UTC");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new UpdateOrganizationRequest(
            "Anon", null, null, null, "EUR", "UTC");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/org_000000000000", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var request = new UpdateOrganizationRequest(
            "Ghost", null, null, null, "EUR", "UTC");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/org_000000000000", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Organization_With_Empty_Name_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new UpdateOrganizationRequest(
            "", null, null, null, "EUR", "UTC");

        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 4: Write ReactivateOrganizationTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class ReactivateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Reactivate_Already_Active_Organization_Returns_409()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{orgId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{orgId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/org_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync($"{BaseUrl}/org_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 5: Write DeleteOrganizationTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class DeleteOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Delete_Without_Confirm_Header_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization2PublicId;

        var response = await client.DeleteAsync($"{BaseUrl}/{orgId}");

        // Missing X-Confirm-Delete header
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_With_Wrong_Confirm_Header_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization2PublicId;

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{orgId}");
        request.Headers.Add("X-Confirm-Delete", "org_wrong_id_here");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization2PublicId;

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{orgId}");
        request.Headers.Add("X-Confirm-Delete", orgId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync($"{BaseUrl}/org_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var fakeId = "org_000000000000";

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{fakeId}");
        request.Headers.Add("X-Confirm-Delete", fakeId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

**Important:** Do NOT write a happy-path delete test that actually deletes org1 or org2 — this would break other tests sharing the same `IClassFixture`. The 404/400/401/403 cases are safe. If a happy-path delete is needed, create a fresh org first via `POST /v1/admin/organizations`, then delete it.

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetOrganizationsTests|FullyQualifiedName~GetOrganizationTests|FullyQualifiedName~UpdateOrganizationTests|FullyQualifiedName~ReactivateOrganizationTests|FullyQualifiedName~DeleteOrganizationTests" -v minimal`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Organizations/
git commit -m "test: add integration tests for admin organization endpoints (get, update, reactivate, delete)"
```

### Task 2.6: Admin Users — GetUsers, GetUser, DeactivateUser, ReactivateUser, DeleteUser

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/GetUsersTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/GetUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/DeactivateUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/ReactivateUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Users/DeleteUserTests.cs`

**Route base:** `/v1/admin/users`. Same admin auth pattern as Task 2.5.

- [ ] **Step 1: Write GetUsersTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class GetUsersTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Get_Users_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Users_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Users_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Write GetUserTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class GetUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Get_User_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User1PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_User_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_User_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync($"{BaseUrl}/usr_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_User_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync($"{BaseUrl}/usr_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 3: Write DeactivateUserTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class DeactivateUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Deactivate_User_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User2PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{userId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_User_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Nonexistent_User_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

Note: Happy path deactivate would make User2 inactive, potentially breaking other tests. Skip it or add a test that deactivates then immediately reactivates in the same test to restore state. The 409 test (already inactive) requires a user that is already inactive — which requires a prior deactivation. Same constraint applies.

- [ ] **Step 4: Write ReactivateUserTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class ReactivateUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Reactivate_Already_Active_User_Returns_409()
    {
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User1PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{userId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_User_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{userId}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_User_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_User_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync($"{BaseUrl}/usr_000000000000/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 5: Write DeleteUserTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class DeleteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Delete_Without_Confirm_Header_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User2PublicId;

        var response = await client.DeleteAsync($"{BaseUrl}/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_With_Wrong_Confirm_Header_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User2PublicId;

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{userId}");
        request.Headers.Add("X-Confirm-Delete", "usr_wrong_id_here");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_User_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User2PublicId;

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{userId}");
        request.Headers.Add("X-Confirm-Delete", userId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_User_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync($"{BaseUrl}/usr_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Nonexistent_User_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var fakeId = "usr_000000000000";

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{fakeId}");
        request.Headers.Add("X-Confirm-Delete", fakeId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Admin.Users" -v minimal`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Users/
git commit -m "test: add integration tests for admin user endpoints (get, deactivate, reactivate, delete)"
```

### Task 2.7: Admin Me

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Me/AdminGetProfileTests.cs`

- [ ] **Step 1: Write AdminGetProfileTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Me;

public sealed class AdminGetProfileTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/me";

    [Fact]
    public async Task Get_Admin_Profile_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Admin_Profile_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Admin_Profile_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~AdminGetProfileTests" -v minimal`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Me/AdminGetProfileTests.cs
git commit -m "test: add integration tests for admin me endpoint"
```

### Task 2.8: Admin Members — AdminGetMembers, AdminDeactivateMember, AdminReactivateMember, AdminRemoveMember

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminGetMembersTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminDeactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminReactivateMemberTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Members/AdminRemoveMemberTests.cs`

**Route base:** `/v1/admin/organizations/{organizationId}/members`. These take `organizationId` as path param (unlike org-scoped endpoints which resolve from JWT).

- [ ] **Step 1: Write AdminGetMembersTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminGetMembersTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string BaseUrl(string orgId) => $"/v1/admin/organizations/{orgId}/members";

    [Fact]
    public async Task Get_Members_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl(factory.Seeder.Organization1PublicId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Members_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl(factory.Seeder.Organization1PublicId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Members_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl("org_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Members_Nonexistent_Org_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl("org_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 2: Write AdminDeactivateMemberTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminDeactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string Url(string orgId, string userId) =>
        $"/v1/admin/organizations/{orgId}/members/{userId}/deactivate";

    [Fact]
    public async Task Deactivate_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var userId = factory.Seeder.AdminUserPublicId;

        var response = await client.PatchAsync(Url(orgId, userId), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync(Url("org_000000000000", "usr_000000000000"), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Member_In_Nonexistent_Org_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync(Url("org_000000000000", "usr_000000000000"), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 3: Write AdminReactivateMemberTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminReactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string Url(string orgId, string userId) =>
        $"/v1/admin/organizations/{orgId}/members/{userId}/reactivate";

    [Fact]
    public async Task Reactivate_Already_Active_Member_Returns_409()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var userId = factory.Seeder.User1PublicId;

        var response = await client.PatchAsync(Url(orgId, userId), null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var userId = factory.Seeder.User1PublicId;

        var response = await client.PatchAsync(Url(orgId, userId), null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync(Url("org_000000000000", "usr_000000000000"), null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 4: Write AdminRemoveMemberTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminRemoveMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string Url(string orgId, string userId) =>
        $"/v1/admin/organizations/{orgId}/members/{userId}";

    [Fact]
    public async Task Remove_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var userId = factory.Seeder.AdminUserPublicId;

        var response = await client.DeleteAsync(Url(orgId, userId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Remove_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync(Url("org_000000000000", "usr_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Remove_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        var response = await client.DeleteAsync(Url(orgId, "usr_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

Note: Do NOT write a happy-path remove test that deletes User1 or AdminUser from org1 — this is a hard delete of the `organization_users` row and would break other tests.

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Admin.Members" -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Members/
git commit -m "test: add integration tests for admin member endpoints (list, deactivate, reactivate, remove)"
```

### Task 2.9: Admin Invites — AdminInviteUser, AdminGetOrgInvites, AdminCancelInvite, AdminGetInvites

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminInviteUserTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminGetOrgInvitesTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminCancelInviteTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Invites/AdminGetInvitesTests.cs`

**Route base:** `/v1/admin/invites` and `/v1/admin/organizations/{organizationId}/invites`.

- [ ] **Step 1: Write AdminInviteUserTests.cs**

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminInviteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string InviteUrl(string orgId) => $"/v1/admin/organizations/{orgId}/invites";

    [Fact]
    public async Task Invite_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new InviteUserRequest("new@example.com", factory.Seeder.DefaultRolePublicId);

        var response = await client.PostAsJsonAsync(InviteUrl(orgId), request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invite_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new InviteUserRequest("new@example.com", "rol_000000000000");

        var response = await client.PostAsJsonAsync(InviteUrl("org_000000000000"), request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_With_Invalid_Email_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new InviteUserRequest("not-an-email", factory.Seeder.DefaultRolePublicId);

        var response = await client.PostAsJsonAsync(InviteUrl(orgId), request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

- [ ] **Step 2: Write AdminGetOrgInvitesTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminGetOrgInvitesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private string Url(string orgId) => $"/v1/admin/organizations/{orgId}/invites";

    [Fact]
    public async Task Get_Org_Invites_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(Url(factory.Seeder.Organization1PublicId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Org_Invites_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(Url(factory.Seeder.Organization1PublicId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Org_Invites_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(Url("org_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Org_Invites_Nonexistent_Org_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(Url("org_000000000000"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

- [ ] **Step 3: Write AdminCancelInviteTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminCancelInviteTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/invites";

    [Fact]
    public async Task Cancel_Nonexistent_Invite_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync($"{BaseUrl}/inv_000000000000/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.PatchAsync($"{BaseUrl}/inv_000000000000/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync($"{BaseUrl}/inv_000000000000/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 4: Write AdminGetInvitesTests.cs**

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminGetInvitesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/invites";

    [Fact]
    public async Task Get_Invites_As_Admin_Returns_200()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Invites_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Invites_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Admin.Invites" -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Invites/
git commit -m "test: add integration tests for admin invite endpoints (invite, list, cancel)"
```

### Task 2.10: Build & Run Full Integration Test Suite

This task verifies all Phase 2 tests pass together.

- [ ] **Step 1: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded (0 warnings/errors due to TreatWarningsAsErrors)

- [ ] **Step 2: Run full integration test suite**

Run: `dotnet test tests/Propentro.Api.Tests -v minimal`
Expected: All tests pass

- [ ] **Step 3: Commit (if any fixes were needed)**

If tests required fixes, commit the fixes as a separate commit.

---

## Phase 3: Unit Tests (Risky Areas)

> **Parallelization note:** Tasks 3.1, 3.2, and 3.3 are fully independent and can run in parallel.

### Task 3.1: Event Handler Unit Tests

**Files:**
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Invites/EventHandlers/InviteCanceledEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberReactivatedEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRemovedEventHandlerTests.cs`
- Create: `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/MemberRolesUpdatedEventHandlerTests.cs`

**Reference:** Existing `MemberDeactivatedEventHandlerTests.cs` in `tests/Propentro.Application.Tests/Features/Organizations/Members/EventHandlers/`.

All 4 event handlers follow the same pattern: they take `IDbConnectionFactory`, `IEmailService`, `IEmailTemplateService` as constructor deps. The key thing to test is the `EventType` property and that the handler can be constructed.

- [ ] **Step 1: Write InviteCanceledEventHandlerTests.cs**

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Invites.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Invites.EventHandlers;

public class InviteCanceledEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_InviteCanceled()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new InviteCanceledEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("invite.canceled");
    }
}
```

- [ ] **Step 2: Write MemberReactivatedEventHandlerTests.cs**

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberReactivatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberReactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberReactivatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.reactivated");
    }
}
```

- [ ] **Step 3: Write MemberRemovedEventHandlerTests.cs**

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberRemovedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberRemoved()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberRemovedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.removed");
    }
}
```

- [ ] **Step 4: Write MemberRolesUpdatedEventHandlerTests.cs**

```csharp
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberRolesUpdatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberRolesUpdated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberRolesUpdatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.roles_updated");
    }
}
```

- [ ] **Step 5: Run tests**

Run: `dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~EventHandler" -v minimal`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add tests/Propentro.Application.Tests/Features/
git commit -m "test: add unit tests for InviteCanceled, MemberReactivated, MemberRemoved, MemberRolesUpdated event handlers"
```

### Task 3.2: Validator Unit Tests

**Files:**
- Create: `tests/Propentro.Application.Tests/Validators/InviteToCurrentOrgValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/AdminInviteUserValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/UpdateMemberRolesValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/CreateOrganizationValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/UpdateOrganizationValidatorTests.cs`

**Reference:** Existing `CreateRoleValidatorTests.cs` in same directory. Uses `FluentValidation.TestHelper` with `TestValidate()` and `ShouldHaveValidationErrorFor()` / `ShouldNotHaveAnyValidationErrors()`.

- [ ] **Step 1: Write InviteToCurrentOrgValidatorTests.cs**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

namespace Propentro.Application.Tests.Validators;

public class InviteToCurrentOrgValidatorTests
{
    private readonly InviteToCurrentOrgValidator _validator = new();

    private static InviteToCurrentOrgCommand CreateValidCommand() =>
        new("user@example.com", "rol_a1b2c3d4e5f6", Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Invalid_Email_Should_Fail(string? email)
    {
        var command = CreateValidCommand() with { Email = email! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_RoleId_Should_Fail(string? roleId)
    {
        var command = CreateValidCommand() with { RoleId = roleId! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
```

- [ ] **Step 2: Write AdminInviteUserValidatorTests.cs**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

namespace Propentro.Application.Tests.Validators;

public class AdminInviteUserValidatorTests
{
    private readonly InviteUserValidator _validator = new();

    private static InviteUserCommand CreateValidCommand() =>
        new("org_a1b2c3d4e5f6", "user@example.com", "rol_a1b2c3d4e5f6", Guid.NewGuid());

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_OrganizationId_Should_Fail(string? orgId)
    {
        var command = CreateValidCommand() with { OrganizationId = orgId! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Invalid_Email_Should_Fail(string? email)
    {
        var command = CreateValidCommand() with { Email = email! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_RoleId_Should_Fail(string? roleId)
    {
        var command = CreateValidCommand() with { RoleId = roleId! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
```

- [ ] **Step 3: Write UpdateMemberRolesValidatorTests.cs**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

namespace Propentro.Application.Tests.Validators;

public class UpdateMemberRolesValidatorTests
{
    private readonly UpdateMemberRolesValidator _validator = new();

    private static UpdateMemberRolesCommand CreateValidCommand() =>
        new("usr_a1b2c3d4e5f6", ["rol_a1b2c3d4e5f6"]);

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("usr_short")]
    public void Invalid_UserId_Format_Should_Fail(string userId)
    {
        var command = CreateValidCommand() with { UserId = userId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Empty_RoleIds_Should_Fail()
    {
        var command = CreateValidCommand() with { RoleIds = [] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }

    [Fact]
    public void More_Than_20_RoleIds_Should_Fail()
    {
        var roleIds = Enumerable.Range(0, 21)
            .Select(i => $"rol_{i:x12}")
            .ToArray();
        var command = CreateValidCommand() with { RoleIds = roleIds };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }

    [Fact]
    public void Invalid_RoleId_Format_Should_Fail()
    {
        var command = CreateValidCommand() with { RoleIds = ["not_a_role_id"] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }
}
```

- [ ] **Step 4: Write CreateOrganizationValidatorTests.cs**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

namespace Propentro.Application.Tests.Validators;

public class CreateOrganizationValidatorTests
{
    private readonly CreateOrganizationValidator _validator = new();

    private static CreateOrganizationCommand CreateValidCommand() =>
        new("Test Org", "test-org", "Contact", "contact@example.com", "EUR", "UTC", "starter");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Name_Should_Fail(string? name)
    {
        var command = CreateValidCommand() with { Name = name! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Exceeding_100_Chars_Should_Fail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("UPPER-CASE")]
    [InlineData("has spaces")]
    [InlineData("special!chars")]
    public void Invalid_Slug_Should_Fail(string? slug)
    {
        var command = CreateValidCommand() with { Slug = slug! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public void Invalid_ContactEmail_Should_Fail()
    {
        var command = CreateValidCommand() with { ContactEmail = "not-an-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Fact]
    public void Null_ContactEmail_Should_Pass()
    {
        var command = CreateValidCommand() with { ContactEmail = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("EURO")]
    public void Invalid_CurrencyCode_Should_Fail(string code)
    {
        var command = CreateValidCommand() with { CurrencyCode = code };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("free")]
    public void Invalid_SubscriptionPlan_Should_Fail(string plan)
    {
        var command = CreateValidCommand() with { SubscriptionPlan = plan };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionPlan);
    }

    [Theory]
    [InlineData("starter")]
    [InlineData("pro")]
    [InlineData("enterprise")]
    public void Valid_SubscriptionPlan_Should_Pass(string plan)
    {
        var command = CreateValidCommand() with { SubscriptionPlan = plan };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SubscriptionPlan);
    }
}
```

- [ ] **Step 5: Write UpdateOrganizationValidatorTests.cs**

```csharp
using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

namespace Propentro.Application.Tests.Validators;

public class UpdateOrganizationValidatorTests
{
    private readonly UpdateOrganizationValidator _validator = new();

    private static UpdateOrganizationCommand CreateValidCommand() =>
        new("org_a1b2c3d4e5f6", "Updated Org", null, null, null, "EUR", "UTC");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Id_Should_Fail(string? id)
    {
        var command = CreateValidCommand() with { Id = id! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Name_Should_Fail(string? name)
    {
        var command = CreateValidCommand() with { Name = name! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Exceeding_200_Chars_Should_Fail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Invalid_ContactEmail_Should_Fail()
    {
        var command = CreateValidCommand() with { ContactEmail = "not-an-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Fact]
    public void Null_ContactEmail_Should_Pass()
    {
        var command = CreateValidCommand() with { ContactEmail = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("EURO")]
    public void Invalid_CurrencyCode_Should_Fail(string code)
    {
        var command = CreateValidCommand() with { CurrencyCode = code };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }
}
```

- [ ] **Step 6: Run tests**

Run: `dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~Validators" -v minimal`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/Propentro.Application.Tests/Validators/
git commit -m "test: add unit tests for complex validators (invite, member roles, org create/update)"
```

### Task 3.3: Entity State Transition Tests (Domain.Tests)

**Context:** The spec lists entity unit tests for `Organization.Deactivate()/Reactivate()`, `User.Deactivate()/Reactivate()`, and `OrganizationUser.Deactivate()/Reactivate()/Remove()`. However, these entities do **not** have state transition methods — the state changes happen inline in command handlers (e.g., `organization.IsActive = false` in `DeactivateOrganizationHandler`). The state guard logic (`AlreadyInactive`, `AlreadyActive`, `CannotDeactivateSelf`) also lives in handlers, not entities.

This means entity unit tests would only test property setters, which provides no value. The state guard logic is covered by integration tests (409 cases in Phase 2). The `Invite` entity already has 8 unit tests covering its `IsExpired()` and `IsPendingAndValid()` methods — these are genuine entity methods with logic worth testing.

**Files:** No new files needed.

- [ ] **Step 1: Verify entities lack state transition methods**

Confirm that `Organization`, `User`, and `OrganizationUser` have no `Deactivate()`, `Reactivate()`, or `Remove()` methods by reading:
- `src/Propentro.Domain/Entities/Identity/Organization.cs`
- `src/Propentro.Domain/Entities/Identity/User.cs`
- `src/Propentro.Domain/Entities/Identity/OrganizationUser.cs`

If any entity DOES have state transition methods (not just properties), write unit tests for those methods following the `InviteTests.cs` pattern.

- [ ] **Step 2: Document the decision**

If entities lack state transition methods, add a note to `docs/testing-strategy.md` under "Conscious Exclusions":

```markdown
### Entity State Transitions
Organization, User, and OrganizationUser entities use simple property setters for state changes
(e.g., `IsActive = false`). The state guard logic lives in command handlers and is tested via
integration tests (409 conflict cases). Only entities with genuine business logic methods
(like `Invite.IsExpired()` and `Invite.IsPendingAndValid()`) have unit tests.
```

- [ ] **Step 3: Commit**

```bash
git add docs/testing-strategy.md
git commit -m "docs: document entity state transition testing decision"
```

---

## Phase 4: Audit Existing Integration Tests

> **Parallelization note:** Tasks 4.1–4.5 are independent and can run in parallel. Each modifies a different test file.

Review each of the 10 existing integration test files and add missing test cases based on the spec's coverage rules. The key gaps to look for:

1. **Cross-tenant 404** — org-scoped GET/PATCH/DELETE endpoints need a test where User1 (org1) tries to access org2 resources
2. **409 state guards** — state transition endpoints need tests for already-in-target-state
3. **Soft delete filtering** — list endpoints should verify deactivated entities don't appear
4. **Missing 401** — every endpoint needs an unauthenticated test
5. **Missing happy path** — some files only have error cases

### Task 4.1: Audit DeactivateMemberTests + GetOrganizationMembersTests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Members/GetOrganizationMembersTests.cs`

**Reference:** Read both files first. `DeactivateMemberTests` currently has 404 and 401. Missing: 403 (User2 trying org1), 409 (already deactivated), cross-tenant 404.

- [ ] **Step 1: Read both existing files**

Read the full content of both files to identify which cases already exist.

- [ ] **Step 2: Add missing cases to DeactivateMemberTests**

Add these tests if they don't already exist:
- `Deactivate_Cross_Tenant_Member_Returns_404` — User1 (org1) tries to deactivate User2 (org2)
- `Deactivate_Self_Returns_409` — User1 tries to deactivate themselves (if `CannotDeactivateSelf` is enforced)

```csharp
[Fact]
public async Task Deactivate_Cross_Tenant_Member_Returns_404()
{
    var client = factory.CreateClientAsUser1();
    var user2Id = factory.Seeder.User2PublicId;

    var response = await client.PatchAsync($"{BaseUrl}/{user2Id}/deactivate", null);

    response.StatusCode.Should().Be(HttpStatusCode.NotFound);
}
```

- [ ] **Step 3: Add missing cases to GetOrganizationMembersTests**

Read the file first. Add if missing:
- `Get_Members_Unauthenticated_Returns_401`
- Happy path (200) if not present

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~DeactivateMemberTests|FullyQualifiedName~GetOrganizationMembersTests" -v minimal`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Members/
git commit -m "test: audit and add missing cases to DeactivateMember and GetOrganizationMembers tests"
```

### Task 4.2: Audit CreateOrganizationTests + DeactivateOrganizationTests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Admin/Organizations/CreateOrganizationTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeactivateOrganizationTests.cs`

- [ ] **Step 1: Read both existing files**

- [ ] **Step 2: Add missing cases to CreateOrganizationTests**

Currently has: happy path, 403, 401. Add if missing:
- `Create_Organization_With_Empty_Name_Returns_400`
- `Create_Organization_With_Duplicate_Slug_Returns_409`

```csharp
[Fact]
public async Task Create_Organization_With_Empty_Name_Returns_400()
{
    var client = factory.CreateClientAsAdmin();
    var request = new CreateOrganizationRequest("", "empty-name", null, null, "EUR", "UTC", "starter");

    var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

- [ ] **Step 3: Add missing cases to DeactivateOrganizationTests**

Currently has: 404, 403. Add if missing:
- `Deactivate_Unauthenticated_Returns_401`
- `Deactivate_Already_Inactive_Organization_Returns_409` (requires deactivating first, may not be safe with shared fixture — add only if there's a safe way)

```csharp
[Fact]
public async Task Deactivate_Organization_Unauthenticated_Returns_401()
{
    var client = factory.CreateAnonymousClient();

    var response = await client.PatchAsync("/v1/admin/organizations/org_000000000000/deactivate", null);

    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~CreateOrganizationTests|FullyQualifiedName~DeactivateOrganizationTests" -v minimal`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/Organizations/
git commit -m "test: audit and add missing cases to CreateOrganization and DeactivateOrganization tests"
```

### Task 4.3: Audit InviteUserTests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Invites/InviteUserTests.cs`

- [ ] **Step 1: Read the file**

Currently has: 401, 400 (invalid email). Missing: happy path, 403.

- [ ] **Step 2: Add missing cases**

Add if missing:
- `Invite_As_User_Without_Permission_Returns_403` — User2 (org2, no members:manage permission) calls org-scoped invite endpoint. Note: User2 doesn't have the DefaultRole so might not have `org:members:manage`. Check if User2 has any roles/permissions seeded. If User2 has no roles, User2 calling the invite endpoint should return 403.

```csharp
[Fact]
public async Task Invite_As_User_Without_Permission_Returns_403()
{
    // User2 is in org2 but has no roles/permissions assigned
    var client = factory.CreateClientAsUser2();
    var request = new InviteUserRequest("new@example.com", "rol_000000000000");

    var response = await client.PostAsJsonAsync(InviteUrl, request);

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~InviteUserTests" -v minimal`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Invites/InviteUserTests.cs
git commit -m "test: audit and add missing cases to InviteUser tests"
```

### Task 4.4: Audit CreateRoleTests + DeleteRoleTests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Roles/DeleteRoleTests.cs`

- [ ] **Step 1: Read both existing files**

- [ ] **Step 2: Add missing cases to each file**

For `CreateRoleTests`, check for: happy path (201), 401, 403, 400 (validation). Add any missing.

For `DeleteRoleTests`, check for: happy path (204 — be careful not to delete seeded role), 401, 403, 404, cross-tenant 404. Add any missing.

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~CreateRoleTests|FullyQualifiedName~DeleteRoleTests" -v minimal`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Roles/
git commit -m "test: audit and add missing cases to CreateRole and DeleteRole tests"
```

### Task 4.5: Audit Settings + Me Tests

**Files:**
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Settings/GetOrganizationSettingsTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Organizations/Settings/UpdateOrganizationSettingsTests.cs`
- Modify: `tests/Propentro.Api.Tests/Features/Me/GetCurrentUserTests.cs`

- [ ] **Step 1: Read all three files**

- [ ] **Step 2: Add missing cases to each**

For settings: check for happy path, 401, 403. Add any missing.
For Me: check for happy path (200), 401. Add any missing.

- [ ] **Step 3: Run tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Settings|FullyQualifiedName~GetCurrentUserTests" -v minimal`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Settings/ tests/Propentro.Api.Tests/Features/Me/
git commit -m "test: audit and add missing cases to Settings and Me tests"
```

### Task 4.6: Full Suite Verification

- [ ] **Step 1: Build**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeded

- [ ] **Step 2: Run full test suite**

Run: `dotnet test Propentro.slnx -v minimal`
Expected: All tests pass across all 5 test projects

- [ ] **Step 3: Commit if any fixes needed**

If any tests required fixes, commit the fixes.
