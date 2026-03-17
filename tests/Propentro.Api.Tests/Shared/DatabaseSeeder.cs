using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Domain.Common;
using Propentro.Domain.Entities.Identity;
using Propentro.Infrastructure.Persistence;

namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Seeds the test database with baseline data required for integration tests.
/// Uses raw DbContext operations with <see cref="DbContext.Database"/> to bypass
/// global query filters and audit field interceptors.
/// </summary>
internal sealed class DatabaseSeeder
{
    private static readonly DateTimeOffset _seedTimestamp = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Organization 1 — EUR, UTC
    public Guid Organization1Id { get; private set; }
    public string Organization1PublicId { get; private set; } = default!;
    public string Organization1ExternalId => TestConstants.DefaultExternalOrgId;
    public string Organization1Slug => "test-org-1";

    // Organization 2 — USD, America/New_York (multi-tenancy isolation)
    public Guid Organization2Id { get; private set; }
    public string Organization2PublicId { get; private set; } = default!;
    public string Organization2ExternalId { get; } = "org_test_secondary";
    public string Organization2Slug => "test-org-2";

    // User 1 — member of org 1, with all permissions
    public Guid User1Id { get; private set; }
    public string User1PublicId { get; private set; } = default!;
    public string User1ExternalId => TestConstants.DefaultExternalUserId;
    public string User1Email => "user1@test.propentro.com";

    // User 2 — member of org 2
    public Guid User2Id { get; private set; }
    public string User2PublicId { get; private set; } = default!;
    public string User2ExternalId { get; } = "user_test_secondary";
    public string User2Email => "user2@test.propentro.com";

    // Admin User — member of org 1, system administrator
    public Guid AdminUserId { get; private set; }
    public string AdminUserPublicId { get; private set; } = default!;
    public string AdminUserExternalId { get; } = "user_test_admin";
    public string AdminUserEmail => "admin@test.propentro.com";

    // Default Role — in org 1, with ALL permissions
    public Guid DefaultRoleId { get; private set; }
    public string DefaultRolePublicId { get; private set; } = default!;

    // System Administrator record
    public Guid SystemAdminId { get; private set; }
    public string SystemAdminPublicId { get; private set; } = default!;

    public async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        // Generate IDs upfront
        Organization1Id = Guid.CreateVersion7();
        Organization1PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix);
        Organization2Id = Guid.CreateVersion7();
        Organization2PublicId = PublicIdGenerator.Generate(Organization.PublicIdPrefix);

        User1Id = Guid.CreateVersion7();
        User1PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix);
        User2Id = Guid.CreateVersion7();
        User2PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix);
        AdminUserId = Guid.CreateVersion7();
        AdminUserPublicId = PublicIdGenerator.Generate(User.PublicIdPrefix);

        DefaultRoleId = Guid.CreateVersion7();
        DefaultRolePublicId = PublicIdGenerator.Generate(Role.PublicIdPrefix);

        SystemAdminId = Guid.CreateVersion7();
        SystemAdminPublicId = PublicIdGenerator.Generate(SystemAdministrator.PublicIdPrefix);

        command.CommandText = BuildSeedSql();
        await command.ExecuteNonQueryAsync();
    }

    private string BuildSeedSql()
    {
        var ts = _seedTimestamp.ToString("o");

        return $"""
            -- Seed permissions (lookup data normally seeded by DB migrations)
            INSERT INTO permissions (id, code, group_name, description, created_at) VALUES
              (gen_random_uuid(), 'org:settings:read',   'Organization Settings', 'View organization settings and branding', '{ts}'),
              (gen_random_uuid(), 'org:settings:update', 'Organization Settings', 'Update organization settings and branding', '{ts}'),
              (gen_random_uuid(), 'org:roles:read',   'Organization Roles', 'View roles and permissions', '{ts}'),
              (gen_random_uuid(), 'org:roles:manage', 'Organization Roles', 'Create and edit roles and permissions', '{ts}'),
              (gen_random_uuid(), 'org:members:read', 'Organization Members', 'View organization members and their roles', '{ts}'),
              (gen_random_uuid(), 'org:members:manage', 'Organization Members', 'Manage organization members and their roles', '{ts}'),
              (gen_random_uuid(), 'documents:read',   'Documents', 'View and download documents', '{ts}'),
              (gen_random_uuid(), 'documents:create', 'Documents', 'Upload documents', '{ts}'),
              (gen_random_uuid(), 'documents:delete', 'Documents', 'Delete documents', '{ts}'),
              (gen_random_uuid(), 'contacts:read',   'Contacts', 'View contact details', '{ts}'),
              (gen_random_uuid(), 'contacts:create', 'Contacts', 'Add new contacts', '{ts}'),
              (gen_random_uuid(), 'contacts:update', 'Contacts', 'Edit contact information', '{ts}'),
              (gen_random_uuid(), 'contacts:delete', 'Contacts', 'Remove contacts', '{ts}'),
              (gen_random_uuid(), 'activities:read',   'Activities', 'View timeline and activity history', '{ts}'),
              (gen_random_uuid(), 'activities:create', 'Activities', 'Create timeline notes and log calls', '{ts}'),
              (gen_random_uuid(), 'activities:update', 'Activities', 'Edit and pin timeline entries', '{ts}'),
              (gen_random_uuid(), 'activities:delete', 'Activities', 'Delete timeline entries', '{ts}'),
              (gen_random_uuid(), 'properties:read',   'Properties', 'View property details', '{ts}'),
              (gen_random_uuid(), 'properties:create', 'Properties', 'Create new properties', '{ts}'),
              (gen_random_uuid(), 'properties:update', 'Properties', 'Edit property information', '{ts}'),
              (gen_random_uuid(), 'properties:delete', 'Properties', 'Delete properties', '{ts}')
            ON CONFLICT (code) DO NOTHING;

            -- Organization 1
            INSERT INTO organizations (id, public_id, slug, name, external_id, auth_provider, currency_code, timezone, is_active, created_at)
            VALUES ('{Organization1Id}', '{Organization1PublicId}', '{Organization1Slug}', 'Test Organization 1', '{Organization1ExternalId}', 'clerk', 'EUR', 'UTC', true, '{ts}');

            -- Organization 2
            INSERT INTO organizations (id, public_id, slug, name, external_id, auth_provider, currency_code, timezone, is_active, created_at)
            VALUES ('{Organization2Id}', '{Organization2PublicId}', '{Organization2Slug}', 'Test Organization 2', '{Organization2ExternalId}', 'clerk', 'USD', 'America/New_York', true, '{ts}');

            -- User 1
            INSERT INTO users (id, public_id, email, first_name, last_name, external_id, auth_provider, is_active, created_at)
            VALUES ('{User1Id}', '{User1PublicId}', '{User1Email}', 'Test', 'User', '{User1ExternalId}', 'clerk', true, '{ts}');

            -- User 2
            INSERT INTO users (id, public_id, email, first_name, last_name, external_id, auth_provider, is_active, created_at)
            VALUES ('{User2Id}', '{User2PublicId}', '{User2Email}', 'Second', 'User', '{User2ExternalId}', 'clerk', true, '{ts}');

            -- Admin User
            INSERT INTO users (id, public_id, email, first_name, last_name, external_id, auth_provider, is_active, created_at)
            VALUES ('{AdminUserId}', '{AdminUserPublicId}', '{AdminUserEmail}', 'Admin', 'User', '{AdminUserExternalId}', 'clerk', true, '{ts}');

            -- Memberships
            INSERT INTO organization_users (organization_id, user_id, status, created_at)
            VALUES ('{Organization1Id}', '{User1Id}', 'active', '{ts}');

            INSERT INTO organization_users (organization_id, user_id, status, created_at)
            VALUES ('{Organization2Id}', '{User2Id}', 'active', '{ts}');

            INSERT INTO organization_users (organization_id, user_id, status, created_at)
            VALUES ('{Organization1Id}', '{AdminUserId}', 'active', '{ts}');

            -- Default Role in org 1 with all permissions
            INSERT INTO roles (id, public_id, organization_id, name, description, is_system, is_active, created_at)
            VALUES ('{DefaultRoleId}', '{DefaultRolePublicId}', '{Organization1Id}', 'Default', 'Default role with all permissions', false, true, '{ts}');

            -- Assign ALL permissions to the default role
            INSERT INTO role_permissions (role_id, permission_id, created_at)
            SELECT '{DefaultRoleId}', p.id, '{ts}'
            FROM permissions p;

            -- Assign default role to User 1 and Admin in org 1
            INSERT INTO organization_user_roles (organization_id, user_id, role_id, assigned_at)
            VALUES ('{Organization1Id}', '{User1Id}', '{DefaultRoleId}', '{ts}');

            INSERT INTO organization_user_roles (organization_id, user_id, role_id, assigned_at)
            VALUES ('{Organization1Id}', '{AdminUserId}', '{DefaultRoleId}', '{ts}');

            -- System Administrator record for Admin User
            INSERT INTO system_administrators (id, public_id, user_id, role, is_active, created_at)
            VALUES ('{SystemAdminId}', '{SystemAdminPublicId}', '{AdminUserId}', 'super_admin', true, '{ts}');
            """;
    }
}
