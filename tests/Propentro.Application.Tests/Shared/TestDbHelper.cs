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
