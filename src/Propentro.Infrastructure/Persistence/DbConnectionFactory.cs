using System.Data;
using Npgsql;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Persistence;

public sealed class DbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(connectionString);
    }
}
