using Npgsql;

namespace Propentro.Application.Common.Interfaces;

public interface IDbConnectionFactory
{
    NpgsqlConnection CreateConnection();
}
