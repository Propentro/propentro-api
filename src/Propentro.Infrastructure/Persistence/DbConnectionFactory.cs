using Microsoft.Extensions.Configuration;
using Npgsql;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Persistence;

public sealed class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Database connection string not configured");

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
