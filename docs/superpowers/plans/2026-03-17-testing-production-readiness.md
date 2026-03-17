# Testing & Production Readiness Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add comprehensive test coverage (architecture, integration, unit) and structural production improvements (correlation ID, health checks, logging, cache invalidation) to the Propentro API.

**Architecture:** Tests follow the existing CQRS pattern — integration tests hit the full HTTP pipeline with Testcontainers PostgreSQL, unit tests cover domain entities and validators in isolation, architecture tests enforce layer dependency rules via NetArchTest. Structural improvements are minimal, focused changes to existing middleware and behaviors.

**Tech Stack:** xUnit, FluentAssertions, NSubstitute, Testcontainers.PostgreSql, Microsoft.AspNetCore.Mvc.Testing, NetArchTest.Rules, Serilog, ASP.NET Core Health Checks

---

## File Structure

### New Files

```
tests/Propentro.Architecture.Tests/
  Propentro.Architecture.Tests.csproj           -- New test project
  LayerDependencyTests.cs                       -- Layer reference rules
  CqrsConventionTests.cs                        -- CQRS pattern enforcement

tests/Propentro.Api.Tests/
  Shared/
    PropentroApiFactory.cs                      -- WebApplicationFactory + Testcontainers
    TestAuthHandler.cs                          -- Fake JWT auth handler
    TestConstants.cs                            -- Known seed data IDs
    DatabaseSeeder.cs                           -- Consistent seed data
    HttpClientExtensions.cs                     -- CreateClientAs() helper
  Features/
    Organizations/
      Settings/
        GetOrganizationSettingsTests.cs
        UpdateOrganizationSettingsTests.cs
      Members/
        GetOrganizationMembersTests.cs
        DeactivateMemberTests.cs
        ReactivateMemberTests.cs
        UpdateMemberRolesTests.cs
      Roles/
        CreateRoleTests.cs
        UpdateRoleTests.cs
        DeleteRoleTests.cs
      Invites/
        InviteUserTests.cs
        CancelInviteTests.cs
        ListInvitesTests.cs
    Admin/
      Organizations/
        CreateOrganizationTests.cs
        UpdateOrganizationTests.cs
        DeactivateOrganizationTests.cs
        ReactivateOrganizationTests.cs
        DeleteOrganizationTests.cs
      Users/
        DeactivateUserTests.cs
        ReactivateUserTests.cs
        DeleteUserTests.cs
      Members/
        AdminDeactivateMemberTests.cs
        AdminReactivateMemberTests.cs
        AdminRemoveMemberTests.cs
      Invites/
        AdminInviteUserTests.cs
    Me/
      GetCurrentUserTests.cs
    Webhooks/
      ClerkWebhookTests.cs

tests/Propentro.Domain.Tests/
  Entities/
    InviteTests.cs
    OrganizationUserTests.cs
  PublicIdGeneratorTests.cs

tests/Propentro.Application.Tests/
  Validators/
    UpdateOrganizationSettingsValidatorTests.cs
    CreateRoleValidatorTests.cs
    CreateOrganizationValidatorTests.cs
    InviteUserValidatorTests.cs
  Services/
    InviteServiceTests.cs

src/Propentro.Application/Common/Interfaces/Caching/
  ICacheInvalidating.cs                         -- Cache invalidation marker interface
```

### Modified Files

```
Propentro.slnx                                             -- Add Architecture.Tests project
Directory.Packages.props                                    -- Add NetArchTest.Rules, AspNetCore.HealthChecks.Redis
tests/Propentro.Api.Tests/Propentro.Api.Tests.csproj        -- Add Testcontainers + Mvc.Testing packages
src/Propentro.Api/Program.cs                                -- Health check expansion, logging improvements
src/Propentro.Api/Middleware/UserContextMiddleware.cs        -- Add correlation ID
src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs -- Add cache invalidation after commit
.github/workflows/ci.yml                                    -- Add Docker service for integration tests
```

---

## Task 1: Architecture Tests — Project Setup

**Files:**
- Create: `tests/Propentro.Architecture.Tests/Propentro.Architecture.Tests.csproj`
- Modify: `Propentro.slnx`
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Add NetArchTest.Rules to Directory.Packages.props**

```xml
<!-- In the Testing section, after NSubstitute -->
<PackageVersion Include="NetArchTest.Rules" Version="1.4.0" />
```

Add after the `<PackageVersion Include="NSubstitute"` line in `Directory.Packages.props`.

- [ ] **Step 2: Create the Architecture test project csproj**

Create `tests/Propentro.Architecture.Tests/Propentro.Architecture.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="NetArchTest.Rules" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Propentro.Api\Propentro.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Add project to solution**

Run: `dotnet sln Propentro.slnx add tests/Propentro.Architecture.Tests/Propentro.Architecture.Tests.csproj`

- [ ] **Step 4: Verify build**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Architecture.Tests/Propentro.Architecture.Tests.csproj Propentro.slnx Directory.Packages.props
git commit -m "chore: add architecture test project with NetArchTest"
```

---

## Task 2: Architecture Tests — Layer Dependency Rules

**Files:**
- Create: `tests/Propentro.Architecture.Tests/LayerDependencyTests.cs`

- [ ] **Step 1: Write layer dependency tests**

Create `tests/Propentro.Architecture.Tests/LayerDependencyTests.cs`:

```csharp
using FluentAssertions;
using NetArchTest.Rules;
using System.Reflection;

namespace Propentro.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private static readonly Assembly _domainAssembly = typeof(Domain.Common.Entity).Assembly;
    private static readonly Assembly _contractsAssembly = typeof(Contracts.Organizations.Settings.OrganizationSettingsResponse).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly _infrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;
    private static readonly Assembly _apiAssembly = typeof(Program).Assembly;

    [Fact]
    public void Domain_Should_Not_Reference_Application()
    {
        Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(_applicationAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Reference_Infrastructure()
    {
        Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Reference_Api()
    {
        Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(_apiAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Domain_Should_Not_Reference_Contracts()
    {
        Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(_contractsAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Contracts_Should_Not_Reference_Domain()
    {
        Types.InAssembly(_contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(_domainAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Contracts_Should_Not_Reference_Application()
    {
        Types.InAssembly(_contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(_applicationAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Contracts_Should_Not_Reference_Infrastructure()
    {
        Types.InAssembly(_contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Contracts_Should_Not_Reference_Api()
    {
        Types.InAssembly(_contractsAssembly)
            .ShouldNot()
            .HaveDependencyOn(_apiAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Reference_Infrastructure()
    {
        Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_Reference_Api()
    {
        Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(_apiAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Reference_Api()
    {
        Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(_apiAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }

    [Fact]
    public void Infrastructure_Should_Not_Reference_Contracts()
    {
        Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(_contractsAssembly.GetName().Name)
            .GetResult()
            .IsSuccessful
            .Should().BeTrue();
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test tests/Propentro.Architecture.Tests --verbosity normal`
Expected: All 12 tests pass. If any fail, it means there's an existing layer violation to fix before proceeding.

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Architecture.Tests/LayerDependencyTests.cs
git commit -m "test: add architecture layer dependency tests"
```

---

## Task 3: Architecture Tests — CQRS Convention Rules

**Files:**
- Create: `tests/Propentro.Architecture.Tests/CqrsConventionTests.cs`

- [ ] **Step 1: Write CQRS convention tests**

Create `tests/Propentro.Architecture.Tests/CqrsConventionTests.cs`:

```csharp
using FluentAssertions;
using NetArchTest.Rules;
using Propentro.Application.Common.Interfaces.Messaging;
using System.Reflection;

namespace Propentro.Architecture.Tests;

public sealed class CqrsConventionTests
{
    private static readonly Assembly _applicationAssembly = typeof(Application.DependencyInjection).Assembly;

    [Fact]
    public void Command_Handlers_Should_Not_Use_Dapper()
    {
        // Command handlers must use EF Core for writes, never Dapper
        // Check for Dapper namespace (not IDbConnectionFactory namespace, which is shared)
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "command handlers must not reference Dapper (use EF Core for writes)");
    }

    [Fact]
    public void Query_Handlers_Should_Not_Use_EfCore()
    {
        // Query handlers must use Dapper for reads, never EF Core
        // Check for EF Core namespace (not IAppDbContext namespace, which shares
        // the Persistence namespace with IDbConnectionFactory used by query handlers)
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "query handlers must not reference EF Core (use Dapper for reads)");
    }

    [Fact]
    public void Command_Records_Should_Be_Sealed()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "all command records must be sealed");
    }

    [Fact]
    public void Query_Records_Should_Be_Sealed()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "all query records must be sealed");
    }

    [Fact]
    public void Handlers_Should_Be_Sealed()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "all handler classes must be sealed");
    }

    [Fact]
    public void Handlers_Should_Not_Be_Public()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameEndingWith("AuthorizationHandler")
            .ShouldNot()
            .BePublic()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "all handler classes must be internal (not public)");
    }
}
```

**Note:** The `Handlers_Should_Not_Be_Public` test excludes `AuthorizationHandler` since those are in Infrastructure and registered via DI — they may need to be public. Adjust the filter if needed after running.

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/Propentro.Architecture.Tests --verbosity normal`
Expected: All tests pass. If any fail, they reveal existing convention violations — fix those first.

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Architecture.Tests/CqrsConventionTests.cs
git commit -m "test: add CQRS convention architecture tests"
```

---

## Task 4: Integration Test Infrastructure — PropentroApiFactory

**Files:**
- Modify: `tests/Propentro.Api.Tests/Propentro.Api.Tests.csproj`
- Create: `tests/Propentro.Api.Tests/Shared/TestConstants.cs`
- Create: `tests/Propentro.Api.Tests/Shared/TestAuthHandler.cs`
- Create: `tests/Propentro.Api.Tests/Shared/PropentroApiFactory.cs`

- [ ] **Step 1: Add missing packages to Api.Tests csproj**

In `tests/Propentro.Api.Tests/Propentro.Api.Tests.csproj`, add to the `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

- [ ] **Step 2: Run `dotnet restore` to verify packages resolve**

Run: `dotnet restore tests/Propentro.Api.Tests/Propentro.Api.Tests.csproj`
Expected: Restore succeeds.

- [ ] **Step 3: Create TestConstants**

Create `tests/Propentro.Api.Tests/Shared/TestConstants.cs`:

```csharp
namespace Propentro.Api.Tests.Shared;

internal static class TestConstants
{
    // External IDs (matching what a real Clerk JWT would carry)
    public const string ExternalUserId1 = "user_test_001";
    public const string ExternalOrgId1 = "org_test_001";
    public const string ExternalUserId2 = "user_test_002";
    public const string ExternalOrgId2 = "org_test_002";

    // System admin
    public const string ExternalAdminUserId = "user_test_admin";
    public const string ExternalAdminOrgId = "org_test_001"; // admin is in org 1

    // Headers used by TestAuthHandler
    public const string TestUserIdHeader = "X-Test-User-Id";
    public const string TestOrgIdHeader = "X-Test-Org-Id";
    public const string TestIsAdminHeader = "X-Test-Is-Admin";
}
```

- [ ] **Step 4: Create TestAuthHandler**

Create `tests/Propentro.Api.Tests/Shared/TestAuthHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Propentro.Api.Tests.Shared;

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestConstants.TestUserIdHeader, out var userId) ||
            string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        if (Request.Headers.TryGetValue(TestConstants.TestOrgIdHeader, out var orgId) &&
            !string.IsNullOrEmpty(orgId))
        {
            claims.Add(new Claim("org_id", orgId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

- [ ] **Step 5: Create PropentroApiFactory**

Create `tests/Propentro.Api.Tests/Shared/PropentroApiFactory.cs`:

```csharp
using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Test-specific IDbConnectionFactory that takes a raw connection string
/// (the real DbConnectionFactory takes IConfiguration which is not available in tests).
/// </summary>
internal sealed class TestDbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}

public sealed class PropentroApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("propentro_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace the real database with Testcontainers
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null)
                services.Remove(dbDescriptor);

            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Replace IDbConnectionFactory with test connection string
            var connFactoryDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IDbConnectionFactory));
            if (connFactoryDescriptor is not null)
                services.Remove(connFactoryDescriptor);

            services.AddSingleton<IDbConnectionFactory>(
                new TestDbConnectionFactory(_dbContainer.GetConnectionString()));

            // Replace JWT auth with test auth handler
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Disable Quartz outbox job in tests
            var quartzDescriptors = services.Where(
                d => d.ServiceType.FullName?.Contains("Quartz") == true).ToList();
            foreach (var descriptor in quartzDescriptors)
                services.Remove(descriptor);
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Run EF Core migrations to create schema
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
```

- [ ] **Step 6: Verify build**

Run: `dotnet build tests/Propentro.Api.Tests`
Expected: Build succeeds. If `DbConnectionFactory` constructor doesn't match, adjust Step 5.

- [ ] **Step 7: Commit**

```bash
git add tests/Propentro.Api.Tests/
git commit -m "feat: add integration test infrastructure (factory, auth handler, constants)"
```

---

## Task 5: Integration Test Infrastructure — Database Seeder

**Files:**
- Create: `tests/Propentro.Api.Tests/Shared/DatabaseSeeder.cs`
- Create: `tests/Propentro.Api.Tests/Shared/HttpClientExtensions.cs`

- [ ] **Step 1: Create DatabaseSeeder**

Create `tests/Propentro.Api.Tests/Shared/DatabaseSeeder.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Domain.Common;
using Propentro.Domain.Entities.Identity;
using Propentro.Infrastructure.Persistence;

namespace Propentro.Api.Tests.Shared;

internal static class DatabaseSeeder
{
    // Known internal IDs for test assertions
    public static Guid Org1Id { get; private set; }
    public static Guid Org2Id { get; private set; }
    public static Guid User1Id { get; private set; }
    public static Guid User2Id { get; private set; }
    public static Guid AdminUserId { get; private set; }
    public static Guid DefaultRoleId { get; private set; }
    public static string Org1PublicId { get; private set; } = default!;
    public static string Org2PublicId { get; private set; } = default!;
    public static string User1PublicId { get; private set; } = default!;
    public static string User2PublicId { get; private set; } = default!;
    public static string AdminUserPublicId { get; private set; } = default!;
    public static string DefaultRolePublicId { get; private set; } = default!;

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Organization 1
        var org1 = new Organization
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix),
            Slug = "test-org-1",
            Name = "Test Organization 1",
            ExternalId = TestConstants.ExternalOrgId1,
            AuthProvider = "clerk",
            CurrencyCode = "EUR",
            Timezone = "UTC",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        Org1Id = org1.Id;
        Org1PublicId = org1.PublicId;

        // Organization 2 (for multi-tenancy isolation tests)
        var org2 = new Organization
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix),
            Slug = "test-org-2",
            Name = "Test Organization 2",
            ExternalId = TestConstants.ExternalOrgId2,
            AuthProvider = "clerk",
            CurrencyCode = "USD",
            Timezone = "America/New_York",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        Org2Id = org2.Id;
        Org2PublicId = org2.PublicId;

        // User 1 (member of org 1)
        var user1 = new User
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix),
            ExternalId = TestConstants.ExternalUserId1,
            AuthProvider = "clerk",
            Email = "user1@test.com",
            FirstName = "Test",
            LastName = "User One",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        User1Id = user1.Id;
        User1PublicId = user1.PublicId;

        // User 2 (member of org 2)
        var user2 = new User
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix),
            ExternalId = TestConstants.ExternalUserId2,
            AuthProvider = "clerk",
            Email = "user2@test.com",
            FirstName = "Test",
            LastName = "User Two",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        User2Id = user2.Id;
        User2PublicId = user2.PublicId;

        // Admin user (system administrator + member of org 1)
        var adminUser = new User
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix),
            ExternalId = TestConstants.ExternalAdminUserId,
            AuthProvider = "clerk",
            Email = "admin@test.com",
            FirstName = "Test",
            LastName = "Admin",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        AdminUserId = adminUser.Id;
        AdminUserPublicId = adminUser.PublicId;

        dbContext.Organizations.AddRange(org1, org2);
        dbContext.Users.AddRange(user1, user2, adminUser);

        // Default role with all permissions for org 1
        var defaultRole = new Role
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Role.PublicIdPrefix),
            OrganizationId = org1.Id,
            Name = "Admin",
            Description = "Full access",
            IsSystem = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        DefaultRoleId = defaultRole.Id;
        DefaultRolePublicId = defaultRole.PublicId;
        dbContext.Roles.Add(defaultRole);

        // Assign all permissions to the default role
        var permissions = await dbContext.Permissions.ToListAsync();
        foreach (var permission in permissions)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = defaultRole.Id,
                PermissionId = permission.Id
            });
        }

        // Memberships
        dbContext.OrganizationUsers.AddRange(
            new OrganizationUser
            {
                OrganizationId = org1.Id,
                UserId = user1.Id,
                Status = OrganizationUser.Statuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrganizationUser
            {
                OrganizationId = org2.Id,
                UserId = user2.Id,
                Status = OrganizationUser.Statuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OrganizationUser
            {
                OrganizationId = org1.Id,
                UserId = adminUser.Id,
                Status = OrganizationUser.Statuses.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });

        // Role assignments
        dbContext.OrganizationUserRoles.AddRange(
            new OrganizationUserRole
            {
                OrganizationId = org1.Id,
                UserId = user1.Id,
                RoleId = defaultRole.Id
            },
            new OrganizationUserRole
            {
                OrganizationId = org1.Id,
                UserId = adminUser.Id,
                RoleId = defaultRole.Id
            });

        // System administrator
        dbContext.SystemAdministrators.Add(new SystemAdministrator
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(SystemAdministrator.PublicIdPrefix),
            UserId = adminUser.Id,
            Role = "super_admin",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }
}
```

**Important:** This seeder uses `SaveChangesAsync` directly — it bypasses `UnitOfWorkBehavior`. The `AppDbContext` global query filters for `OrganizationId` are applied via EF Core, but seed data inserts bypass those filters since we're writing directly.

- [ ] **Step 2: Create HttpClientExtensions**

Create `tests/Propentro.Api.Tests/Shared/HttpClientExtensions.cs`:

```csharp
namespace Propentro.Api.Tests.Shared;

internal static class HttpClientExtensions
{
    /// <summary>
    /// Creates an HttpClient authenticated as the specified user/org.
    /// </summary>
    public static HttpClient CreateClientAs(
        this PropentroApiFactory factory,
        string externalUserId,
        string externalOrgId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestConstants.TestUserIdHeader, externalUserId);
        client.DefaultRequestHeaders.Add(TestConstants.TestOrgIdHeader, externalOrgId);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient authenticated as User 1 in Org 1.
    /// </summary>
    public static HttpClient CreateClientAsUser1(this PropentroApiFactory factory) =>
        factory.CreateClientAs(TestConstants.ExternalUserId1, TestConstants.ExternalOrgId1);

    /// <summary>
    /// Creates an HttpClient authenticated as User 2 in Org 2.
    /// </summary>
    public static HttpClient CreateClientAsUser2(this PropentroApiFactory factory) =>
        factory.CreateClientAs(TestConstants.ExternalUserId2, TestConstants.ExternalOrgId2);

    /// <summary>
    /// Creates an HttpClient authenticated as the system admin.
    /// </summary>
    public static HttpClient CreateClientAsAdmin(this PropentroApiFactory factory) =>
        factory.CreateClientAs(TestConstants.ExternalAdminUserId, TestConstants.ExternalAdminOrgId);

    /// <summary>
    /// Creates an unauthenticated HttpClient.
    /// </summary>
    public static HttpClient CreateAnonymousClient(this PropentroApiFactory factory) =>
        factory.CreateClient();
}
```

- [ ] **Step 3: Update PropentroApiFactory to call seeder**

In `PropentroApiFactory.InitializeAsync()`, add after `MigrateAsync()`:

```csharp
await DatabaseSeeder.SeedAsync(Services);
```

- [ ] **Step 4: Verify build**

Run: `dotnet build tests/Propentro.Api.Tests`
Expected: Build succeeds. If entity properties don't match (e.g., missing `PublicId`, `FullName`), adjust the seeder to match actual entity definitions.

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Shared/
git commit -m "feat: add database seeder and HTTP client helpers for integration tests"
```

---

## Task 6: Integration Tests — Organization Settings (First Endpoint)

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Settings/GetOrganizationSettingsTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Settings/UpdateOrganizationSettingsTests.cs`

This is the first integration test — it validates the entire infrastructure (factory, seeder, auth bypass, Testcontainers) works end-to-end.

- [ ] **Step 1: Write GetOrganizationSettings tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Settings/GetOrganizationSettingsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Api.Tests.Features.Organizations.Settings;

public sealed class GetOrganizationSettingsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Settings_Returns_Organization_Settings()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Name.Should().Be("Test Organization 1");
        settings.CurrencyCode.Should().Be("EUR");
    }

    [Fact]
    public async Task Get_Settings_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Settings_Returns_Own_Organization_Not_Other()
    {
        // Arrange — User 2 is in Org 2
        var client = factory.CreateClientAsUser2();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Name.Should().Be("Test Organization 2");
    }
}
```

- [ ] **Step 2: Run tests with Docker running**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetOrganizationSettingsTests" --verbosity normal`
Expected: All 3 tests pass. This validates the entire test infrastructure works.

**If tests fail:** Common issues:
- Docker not running → start Docker Desktop
- Migration failure → check EF Core migration files exist and are compatible
- Auth bypass not working → verify `TestAuthHandler` claim types match what `UserContextMiddleware` expects
- Seeder fails → check entity property names match domain model

- [ ] **Step 3: Write UpdateOrganizationSettings tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Settings/UpdateOrganizationSettingsTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Api.Tests.Features.Organizations.Settings;

public sealed class UpdateOrganizationSettingsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Update_Settings_Returns_Updated_Settings()
    {
        var client = factory.CreateClientAsUser1();
        var request = new UpdateOrganizationSettingsRequest(
            Name: "Updated Org Name",
            ContactName: "New Contact",
            ContactEmail: "new@test.com",
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "USD",
            Timezone: "America/New_York");

        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings!.Name.Should().Be("Updated Org Name");
        settings.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public async Task Update_Settings_With_Invalid_Email_Returns_400()
    {
        var client = factory.CreateClientAsUser1();
        var request = new UpdateOrganizationSettingsRequest(
            Name: "Valid Name",
            ContactName: null,
            ContactEmail: "not-an-email",
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Settings_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new UpdateOrganizationSettingsRequest(
            Name: "Hacked",
            ContactName: null,
            ContactEmail: null,
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Note:** Check the actual `UpdateOrganizationSettingsRequest` record in `Contracts/Organizations/Settings/` to verify the constructor parameters match. Adjust if needed.

- [ ] **Step 4: Run all settings tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Settings" --verbosity normal`
Expected: All 6 tests pass.

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Settings/
git commit -m "test: add integration tests for organization settings endpoints"
```

---

## Task 7: Integration Tests — Members Endpoints

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/GetOrganizationMembersTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs`

- [ ] **Step 1: Write GetOrganizationMembers tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Members/GetOrganizationMembersTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class GetOrganizationMembersTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Members_Returns_Paged_Result()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync("/v1/organizations/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Members_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/v1/organizations/members");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Members_Only_Returns_Own_Org_Members()
    {
        // User 2 in Org 2 should not see Org 1's members
        var client = factory.CreateClientAsUser2();

        var response = await client.GetAsync("/v1/organizations/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Response should not include User 1 (Org 1 member)
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("user1@test.com");
    }
}
```

- [ ] **Step 2: Write DeactivateMember tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Members/DeactivateMemberTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class DeactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Deactivate_Nonexistent_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.PatchAsync(
            "/v1/organizations/members/usr_000000000000/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync(
            $"/v1/organizations/members/{DatabaseSeeder.User1PublicId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Note:** The exact URL pattern for member operations depends on the actual endpoint routes in `MembersEndpoints.cs`. Check the route pattern and adjust (e.g., it might be `/v1/organizations/members/{id}/deactivate` or `/v1/members/{id}/deactivate`).

- [ ] **Step 3: Run member tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Members" --verbosity normal`
Expected: All tests pass. Adjust URL paths if needed.

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Members/
git commit -m "test: add integration tests for organization member endpoints"
```

---

## Task 8: Integration Tests — Roles Endpoints

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Roles/DeleteRoleTests.cs`

- [ ] **Step 1: Write CreateRole tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Roles/CreateRoleTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class CreateRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Create_Role_With_Valid_Data_Returns_201()
    {
        var client = factory.CreateClientAsUser1();

        // Check the actual CreateRoleRequest shape in Contracts
        var request = new { Name = "Custom Role", Description = "Test role" };

        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Role_With_Empty_Name_Returns_400()
    {
        var client = factory.CreateClientAsUser1();
        var request = new { Name = "", Description = "Test" };

        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Role_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new { Name = "Hacked Role", Description = "No" };

        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Write DeleteRole tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Roles/DeleteRoleTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class DeleteRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Delete_Nonexistent_Role_Returns_404()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.DeleteAsync("/v1/organizations/roles/rol_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Role_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.DeleteAsync("/v1/organizations/roles/rol_000000000000");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

**Note:** Check actual route patterns in `RolesEndpoints.cs`. Also check if DELETE requires `X-Confirm-Delete` header per the spec.

- [ ] **Step 3: Run role tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Roles" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Roles/
git commit -m "test: add integration tests for organization role endpoints"
```

---

## Task 9: Integration Tests — Invite Endpoints

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Organizations/Invites/InviteUserTests.cs`

- [ ] **Step 1: Write invite tests**

Create `tests/Propentro.Api.Tests/Features/Organizations/Invites/InviteUserTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Invites;

public sealed class InviteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Invite_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();
        var request = new { Email = "new@test.com", RoleId = DatabaseSeeder.DefaultRolePublicId };

        var response = await client.PostAsJsonAsync("/v1/organizations/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_With_Invalid_Email_Returns_400()
    {
        var client = factory.CreateClientAsUser1();
        var request = new { Email = "not-an-email", RoleId = DatabaseSeeder.DefaultRolePublicId };

        var response = await client.PostAsJsonAsync("/v1/organizations/invites", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
```

**Note:** The actual invite endpoint route and request shape depends on `InviteEndpoints.cs` and the invite request DTO. Adjust after checking.

- [ ] **Step 2: Run invite tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Invites" --verbosity normal`
Expected: Tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Organizations/Invites/
git commit -m "test: add integration tests for invite endpoints"
```

---

## Task 10: Integration Tests — Me Endpoint

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Me/GetCurrentUserTests.cs`

- [ ] **Step 1: Write Me endpoint tests**

Create `tests/Propentro.Api.Tests/Features/Me/GetCurrentUserTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Me;

public sealed class GetCurrentUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Me_Returns_Current_User()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.GetAsync("/v1/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Me_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.GetAsync("/v1/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Run test**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~GetCurrentUserTests" --verbosity normal`
Expected: Both tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Me/
git commit -m "test: add integration tests for /me endpoint"
```

---

## Task 11: Integration Tests — Admin Endpoints

**Files:**
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/CreateOrganizationTests.cs`
- Create: `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeactivateOrganizationTests.cs`

Admin endpoints require system administrator auth. The seeder creates a system admin user.

- [ ] **Step 1: Write admin organization tests**

Create `tests/Propentro.Api.Tests/Features/Admin/Organizations/CreateOrganizationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class CreateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Create_Organization_As_Admin_Succeeds()
    {
        var client = factory.CreateClientAsAdmin();

        // Check actual CreateOrganizationRequest shape in Contracts
        var request = new
        {
            Name = "New Test Org",
            Slug = "new-test-org",
            ContactEmail = "contact@neworg.com"
        };

        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var request = new
        {
            Name = "Unauthorized Org",
            Slug = "unauthorized-org",
            ContactEmail = "no@way.com"
        };

        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var request = new { Name = "Hacked", Slug = "hacked" };

        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

- [ ] **Step 2: Write deactivate organization tests**

Create `tests/Propentro.Api.Tests/Features/Admin/Organizations/DeactivateOrganizationTests.cs`:

```csharp
using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class DeactivateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Deactivate_Nonexistent_Organization_Returns_404()
    {
        var client = factory.CreateClientAsAdmin();

        var response = await client.PatchAsync(
            "/v1/admin/organizations/org_000000000000/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Organization_As_Regular_User_Returns_403()
    {
        var client = factory.CreateClientAsUser1();

        var response = await client.PatchAsync(
            $"/v1/admin/organizations/{DatabaseSeeder.Org1PublicId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
```

**Note:** Check `AdminOrganizationEndpoints.cs` for exact route patterns. They might use `{organizationId}` instead of `{id}`.

- [ ] **Step 3: Run admin tests**

Run: `dotnet test tests/Propentro.Api.Tests --filter "FullyQualifiedName~Admin" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add tests/Propentro.Api.Tests/Features/Admin/
git commit -m "test: add integration tests for admin organization endpoints"
```

---

## Task 12: Domain Unit Tests — Invite Entity

**Files:**
- Create: `tests/Propentro.Domain.Tests/Entities/InviteTests.cs`

- [ ] **Step 1: Write Invite entity tests**

Create `tests/Propentro.Domain.Tests/Entities/InviteTests.cs`:

```csharp
using FluentAssertions;
using Propentro.Domain.Entities.Identity;

namespace Propentro.Domain.Tests.Entities;

public sealed class InviteTests
{
    [Fact]
    public void IsExpired_Returns_True_When_Past_Expiry()
    {
        var invite = new Invite
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        invite.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Returns_False_When_Before_Expiry()
    {
        var invite = new Invite
        {
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        invite.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_Returns_False_When_No_ExpiresAt()
    {
        var invite = new Invite { ExpiresAt = null };

        invite.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPendingAndValid_Returns_True_When_Pending_And_Not_Expired()
    {
        var invite = new Invite
        {
            Status = Invite.Statuses.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        invite.IsPendingAndValid(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Accepted()
    {
        var invite = new Invite
        {
            Status = Invite.Statuses.Accepted,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        invite.IsPendingAndValid(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Pending_But_Expired()
    {
        var invite = new Invite
        {
            Status = Invite.Statuses.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        invite.IsPendingAndValid(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Canceled()
    {
        var invite = new Invite
        {
            Status = Invite.Statuses.Canceled,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        invite.IsPendingAndValid(DateTimeOffset.UtcNow).Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/Propentro.Domain.Tests --filter "FullyQualifiedName~InviteTests" --verbosity normal`
Expected: All 7 tests pass.

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Domain.Tests/Entities/InviteTests.cs
git commit -m "test: add unit tests for Invite entity logic"
```

---

## Task 13: Domain Unit Tests — PublicIdGenerator

**Files:**
- Create: `tests/Propentro.Domain.Tests/PublicIdGeneratorTests.cs`

- [ ] **Step 1: Write PublicIdGenerator tests**

Create `tests/Propentro.Domain.Tests/PublicIdGeneratorTests.cs`:

```csharp
using FluentAssertions;
using Propentro.Domain.Common;

namespace Propentro.Domain.Tests;

public sealed class PublicIdGeneratorTests
{
    [Fact]
    public void Generate_Returns_Prefixed_Id_With_Correct_Format()
    {
        var id = PublicIdGenerator.Generate("prp");

        id.Should().StartWith("prp_");
        id.Should().HaveLength(16); // "prp_" (4) + 12 hex chars
    }

    [Fact]
    public void Generate_Returns_Unique_Ids()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => PublicIdGenerator.Generate("tst"))
            .ToHashSet();

        ids.Should().HaveCount(100);
    }

    [Fact]
    public void Generate_Returns_Lowercase_Hex()
    {
        var id = PublicIdGenerator.Generate("tst");
        var hexPart = id["tst_".Length..];

        hexPart.Should().MatchRegex("^[0-9a-f]{12}$");
    }

    [Theory]
    [InlineData("org")]
    [InlineData("usr")]
    [InlineData("inv")]
    [InlineData("prp")]
    public void Generate_Uses_Provided_Prefix(string prefix)
    {
        var id = PublicIdGenerator.Generate(prefix);

        id.Should().StartWith($"{prefix}_");
    }
}
```

- [ ] **Step 2: Run tests**

Run: `dotnet test tests/Propentro.Domain.Tests --filter "FullyQualifiedName~PublicIdGeneratorTests" --verbosity normal`
Expected: All 7 tests pass (4 theory cases + 3 facts).

- [ ] **Step 3: Commit**

```bash
git add tests/Propentro.Domain.Tests/PublicIdGeneratorTests.cs
git commit -m "test: add unit tests for PublicIdGenerator"
```

---

## Task 14: Application Unit Tests — Validators

**Files:**
- Create: `tests/Propentro.Application.Tests/Validators/UpdateOrganizationSettingsValidatorTests.cs`
- Create: `tests/Propentro.Application.Tests/Validators/CreateRoleValidatorTests.cs`

- [ ] **Step 1: Write UpdateOrganizationSettings validator tests**

Create `tests/Propentro.Application.Tests/Validators/UpdateOrganizationSettingsValidatorTests.cs`:

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;

namespace Propentro.Application.Tests.Validators;

public sealed class UpdateOrganizationSettingsValidatorTests
{
    private readonly UpdateOrganizationSettingsValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        var command = new UpdateOrganizationSettingsCommand(
            Name: "Test Org",
            ContactName: "John",
            ContactEmail: "john@test.com",
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var command = new UpdateOrganizationSettingsCommand(
            Name: "",
            ContactName: null,
            ContactEmail: null,
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var command = new UpdateOrganizationSettingsCommand(
            Name: "Valid",
            ContactName: null,
            ContactEmail: "not-an-email",
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Fact]
    public void Invalid_Country_Code_Length_Fails()
    {
        var command = new UpdateOrganizationSettingsCommand(
            Name: "Valid",
            ContactName: null,
            ContactEmail: null,
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: "USA",
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "EUR",
            Timezone: "UTC");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Fact]
    public void Empty_CurrencyCode_Fails()
    {
        var command = new UpdateOrganizationSettingsCommand(
            Name: "Valid",
            ContactName: null,
            ContactEmail: null,
            ContactPhone: null,
            AddressLine1: null,
            AddressLine2: null,
            City: null,
            Region: null,
            PostalCode: null,
            CountryCode: null,
            LegalName: null,
            TaxNumber: null,
            TaxCountry: null,
            CurrencyCode: "",
            Timezone: "UTC");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }
}
```

**Note:** Verify `UpdateOrganizationSettingsCommand` constructor parameters match the actual command definition. Check `src/Propentro.Application/Features/Organizations/Settings/Commands/UpdateOrganizationSettings/`.

- [ ] **Step 2: Run validator tests**

Run: `dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~UpdateOrganizationSettingsValidatorTests" --verbosity normal`
Expected: All 5 tests pass.

- [ ] **Step 3: Write CreateRole validator tests**

Create `tests/Propentro.Application.Tests/Validators/CreateRoleValidatorTests.cs`. Check the actual `CreateRoleCommand` and `CreateRoleValidator` definitions first, then write tests for:
- Valid input passes
- Empty name fails
- Name exceeding max length fails

Follow the same pattern as the settings validator tests above.

- [ ] **Step 4: Run all validator tests**

Run: `dotnet test tests/Propentro.Application.Tests --filter "FullyQualifiedName~Validator" --verbosity normal`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add tests/Propentro.Application.Tests/Validators/
git commit -m "test: add unit tests for FluentValidation validators"
```

---

## Task 15: Correlation ID in UserContextMiddleware

**Files:**
- Modify: `src/Propentro.Api/Middleware/UserContextMiddleware.cs`

- [ ] **Step 1: Add correlation ID to UserContextMiddleware**

In `src/Propentro.Api/Middleware/UserContextMiddleware.cs`, update the `InvokeAsync` method:

```csharp
public async Task InvokeAsync(HttpContext context, IUserContext userContext)
{
    // Correlation ID — use incoming header or generate new
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString("N");
    context.Response.Headers["X-Correlation-Id"] = correlationId;

    // Initialize user context (resolves external IDs to internal IDs)
    if (userContext is IUserContextInitializer initializer && !initializer.IsInitialized)
    {
        await initializer.InitializeAsync(context.RequestAborted);
    }

    // Push resolved IDs to log context for structured logging
    var userId = userContext.IsAuthenticated
        ? userContext.UserId.ToString()
        : "anonymous";
    var organizationId = userContext.IsAuthenticated && userContext.OrganizationId != Guid.Empty
        ? userContext.OrganizationId.ToString()
        : "none";
    var externalUserId = userContext.ExternalUserId ?? "none";

    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("UserId", userId))
    using (LogContext.PushProperty("OrganizationId", organizationId))
    using (LogContext.PushProperty("ExternalUserId", externalUserId))
    {
        await next(context);
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build src/Propentro.Api`
Expected: Build succeeds with 0 errors, 0 warnings.

- [ ] **Step 3: Run existing tests to ensure no regression**

Run: `dotnet test Propentro.slnx --verbosity normal`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Api/Middleware/UserContextMiddleware.cs
git commit -m "feat: add correlation ID to UserContextMiddleware"
```

---

## Task 16: Health Check Expansion

**Files:**
- Modify: `Directory.Packages.props`
- Modify: `src/Propentro.Api/Program.cs`

- [ ] **Step 1: Add Redis health check package to Directory.Packages.props**

Add in the Health Checks section:

```xml
<PackageVersion Include="AspNetCore.HealthChecks.Redis" Version="9.0.0" />
```

- [ ] **Step 2: Add package reference to Api.csproj**

Check `src/Propentro.Api/Propentro.Api.csproj` — add if not already there:

```xml
<PackageReference Include="AspNetCore.HealthChecks.Redis" />
```

- [ ] **Step 3: Update health check registration in Program.cs**

Replace the existing health check section in `Program.cs`:

```csharp
// Health Checks
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!,
        name: "postgresql",
        tags: ["db", "ready"]);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    healthChecksBuilder.AddRedis(redisConnectionString,
        name: "redis",
        tags: ["cache", "ready"]);
}
```

- [ ] **Step 4: Update health check endpoints in Program.cs**

Replace the existing `MapHealthChecks` call:

```csharp
// Health check endpoints (no auth required)
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();
```

Add `using Microsoft.AspNetCore.Diagnostics.HealthChecks;` at the top of Program.cs if not already present.

- [ ] **Step 5: Build and verify**

Run: `dotnet build src/Propentro.Api`
Expected: Build succeeds.

- [ ] **Step 6: Run tests**

Run: `dotnet test Propentro.slnx --verbosity normal`
Expected: All tests pass.

- [ ] **Step 7: Commit**

```bash
git add Directory.Packages.props src/Propentro.Api/Propentro.Api.csproj src/Propentro.Api/Program.cs
git commit -m "feat: expand health checks with /health/ready endpoint and Redis check"
```

---

## Task 17: Structured Logging Improvements

**Files:**
- Modify: `src/Propentro.Api/Program.cs`

- [ ] **Step 1: Update Serilog configuration in Program.cs**

Replace the Serilog logger configuration (lines 15-25) with:

```csharp
var isProduction = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production";

var logConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "Propentro.Api");

if (isProduction)
{
    logConfig.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
}
else
{
    logConfig.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture);
}

Log.Logger = logConfig.CreateLogger();
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build src/Propentro.Api`
Expected: Build succeeds.

- [ ] **Step 3: Run tests**

Run: `dotnet test Propentro.slnx --verbosity normal`
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/Propentro.Api/Program.cs
git commit -m "feat: add JSON structured logging for production environment"
```

---

## Task 18: Automatic Cache Invalidation

**Files:**
- Create: `src/Propentro.Application/Common/Interfaces/Caching/ICacheInvalidating.cs`
- Modify: `src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs`

- [ ] **Step 1: Create ICacheInvalidating interface**

Create `src/Propentro.Application/Common/Interfaces/Caching/ICacheInvalidating.cs`:

```csharp
namespace Propentro.Application.Common.Interfaces.Caching;

/// <summary>
/// Marker interface for commands that should invalidate specific cache entries after successful commit.
/// Implement on command records to declare which cache keys should be cleared.
/// </summary>
public interface ICacheInvalidating
{
    /// <summary>
    /// Cache keys to invalidate after successful commit.
    /// For org-scoped commands: use bare key names (e.g., "organization-settings").
    /// The behavior automatically prefixes with "org:{OrganizationId}:".
    /// For admin commands: use fully-qualified keys and set KeysAreFullyQualified to true.
    /// </summary>
    IReadOnlyList<string> CacheKeysToInvalidate { get; }

    /// <summary>
    /// When true, the behavior uses keys as-is without adding the org prefix.
    /// Default: false (behavior adds "org:{OrganizationId}:" prefix).
    /// </summary>
    bool KeysAreFullyQualified => false;
}
```

- [ ] **Step 2: Update UnitOfWorkBehavior to support cache invalidation**

Replace `src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs`:

```csharp
using MediatR;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Caching;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps command execution in a database transaction.
/// Automatically calls SaveChangesAsync and commits on success, rolls back on failure.
/// After successful commit, invalidates cache keys if the command implements ICacheInvalidating.
/// Handlers should NOT call SaveChangesAsync — this behavior handles persistence.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IAppDbContext dbContext,
    ICacheService cache,
    IUserContext userContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
        {
            return await next(cancellationToken);
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            Log.Debug("Transaction started for {RequestName}", typeof(TRequest).Name);

            var response = await next(cancellationToken);

            if (IsFailureResult(response))
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Debug("Transaction rolled back for {RequestName} - failure result", typeof(TRequest).Name);
                return response;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            Log.Debug("Transaction committed for {RequestName}", typeof(TRequest).Name);

            await InvalidateCacheAsync(request, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Transaction rolled back for {RequestName} - exception", typeof(TRequest).Name);
            throw;
        }
    }

    private async Task InvalidateCacheAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (request is not ICacheInvalidating cacheInvalidating)
            return;

        foreach (var key in cacheInvalidating.CacheKeysToInvalidate)
        {
            var cacheKey = cacheInvalidating.KeysAreFullyQualified
                ? key
                : $"org:{userContext.OrganizationId}:{key}";

            await cache.RemoveAsync(cacheKey, cancellationToken);
            Log.Debug("Cache invalidated: {CacheKey}", cacheKey);
        }
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).GetInterfaces().Any(i =>
            i == typeof(ICommand) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
    }

    private static bool IsFailureResult(TResponse response)
    {
        return response is Result { IsFailure: true };
    }
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build Propentro.slnx`
Expected: Build succeeds.

- [ ] **Step 4: Run all tests**

Run: `dotnet test Propentro.slnx --verbosity normal`
Expected: All tests pass. The `UnitOfWorkBehavior` change is backward-compatible — commands without `ICacheInvalidating` are unaffected.

- [ ] **Step 5: Commit**

```bash
git add src/Propentro.Application/Common/Interfaces/Caching/ICacheInvalidating.cs src/Propentro.Application/Common/Behaviors/UnitOfWorkBehavior.cs
git commit -m "feat: add automatic cache invalidation via ICacheInvalidating interface"
```

---

## Task 19: CI Pipeline — Docker Service for Integration Tests

**Files:**
- Modify: `.github/workflows/ci.yml`

- [ ] **Step 1: Update CI workflow to include Docker for Testcontainers**

Replace `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main, staging]
  pull_request:
    branches: [main, staging]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true

jobs:
  build-and-test:
    name: Build & Test
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4

      - name: Restore
        run: dotnet restore Propentro.slnx

      - name: Build
        run: dotnet build Propentro.slnx --configuration Release --no-restore

      - name: Test
        run: dotnet test Propentro.slnx --configuration Release --no-restore --no-build --verbosity normal
```

**Note:** Ubuntu runners in GitHub Actions already have Docker installed and running. Testcontainers will pull `postgres:17-alpine` automatically. No explicit Docker service is needed — the existing workflow already runs `dotnet test` which now includes integration tests. The only consideration is that the first run will be slower due to Docker image pull. If tests are very slow, consider adding a Docker layer caching step in the future.

The CI already has the test step. The key change is that now there are actual tests to run. Verify the workflow is correct as-is.

- [ ] **Step 2: Verify no CI changes needed**

The existing CI workflow already runs `dotnet test Propentro.slnx` which will pick up all test projects including the new Architecture tests and integration tests. Ubuntu runners have Docker pre-installed for Testcontainers.

If the architecture test project was added to the solution (Task 1), it will be discovered automatically.

- [ ] **Step 3: Commit (if any changes were made)**

If the CI file needed changes:
```bash
git add .github/workflows/ci.yml
git commit -m "ci: ensure Docker available for Testcontainers integration tests"
```

---

## Task 20: Final Verification — Full Test Suite

- [ ] **Step 1: Run the complete test suite**

Run: `dotnet test Propentro.slnx --verbosity normal`
Expected: All tests pass — architecture tests, integration tests, domain unit tests, application unit tests.

- [ ] **Step 2: Run CI-equivalent build**

Run: `dotnet restore Propentro.slnx && dotnet build Propentro.slnx --configuration Release --no-restore && dotnet test Propentro.slnx --configuration Release --no-restore --no-build --verbosity normal`
Expected: Full CI pipeline passes locally.

- [ ] **Step 3: Final commit if any cleanup needed**

```bash
git add -A
git commit -m "chore: final cleanup for testing & production readiness"
```

---

## Execution Notes

### Prerequisites
- **Docker Desktop** must be running for integration tests (Testcontainers requirement)
- All packages are already in `Directory.Packages.props` — no external registry access needed beyond NuGet

### Route Verification
Integration tests assume specific URL patterns. Before implementing each test task, read the corresponding endpoint file to verify:
- Route patterns (e.g., `/v1/organizations/members/{id}/deactivate` vs `/v1/members/{id}/deactivate`)
- Request/response DTO shapes (check `Contracts/` directory)
- HTTP methods (GET, POST, PATCH, DELETE)
- Required headers (e.g., `X-Confirm-Delete` for hard deletes)

### Test Isolation
All test classes share one `PropentroApiFactory` instance (via `IClassFixture`). Tests that mutate data may affect other tests in the same class. Strategies:
- Use unique records per test (e.g., create a new role with a unique name)
- Test read-only operations against seed data
- Test mutations against newly created records, not seed data

---

## Deferred Test Coverage

The spec lists additional test files that are **not covered in this plan**. These should be added as follow-up tasks after the core infrastructure and first batch of tests are working:

### Integration Tests (deferred)
- `Features/Organizations/Members/ReactivateMemberTests.cs`
- `Features/Organizations/Members/UpdateMemberRolesTests.cs`
- `Features/Organizations/Invites/CancelInviteTests.cs`
- `Features/Organizations/Invites/ListInvitesTests.cs`
- `Features/Admin/Users/DeactivateUserTests.cs`
- `Features/Admin/Users/ReactivateUserTests.cs`
- `Features/Admin/Users/DeleteUserTests.cs`
- `Features/Admin/Members/AdminDeactivateMemberTests.cs`
- `Features/Admin/Members/AdminReactivateMemberTests.cs`
- `Features/Admin/Members/AdminRemoveMemberTests.cs`
- `Features/Admin/Invites/AdminInviteUserTests.cs`
- `Features/Admin/Me/AdminMeTests.cs` (admin `/v1/admin/me` endpoint)
- `Features/Webhooks/ClerkWebhookTests.cs`

### Domain Unit Tests (deferred)
- `Entities/OrganizationTests.cs` — deactivate, reactivate
- `Entities/OrganizationUserTests.cs` — status transitions
- `Entities/RoleTests.cs` — permission management
- `Entities/PropertyTests.cs` — creation, update events
- `Entities/ContactTests.cs` — archive, delete events
- `ValueObjects/MoneyTests.cs`
- `ValueObjects/DateRangeTests.cs`

### Application Unit Tests (deferred)
- `Validators/CreateOrganizationValidatorTests.cs`
- `Validators/InviteUserValidatorTests.cs`
- `Services/InviteServiceTests.cs`

These tests follow the same patterns established in Tasks 6-14. Once the infrastructure is proven, adding them is straightforward.
