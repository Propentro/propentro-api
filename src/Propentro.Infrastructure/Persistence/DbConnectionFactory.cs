using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// Creates PostgreSQL database connections for Dapper queries.
/// </summary>
public sealed class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Database connection string not configured");

    public DbConnection CreateConnection() => new NpgsqlConnection(_connectionString);
}
