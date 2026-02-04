using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// Creates PostgreSQL database connections for Dapper queries.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Database connection string not configured");
    }

    /// <inheritdoc />
    public DbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
