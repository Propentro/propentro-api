using System.Data.Common;

namespace Propentro.Application.Common.Interfaces.Persistence;

/// <summary>
/// Factory for creating database connections for Dapper queries.
/// Provides raw ADO.NET connections for high-performance read operations.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a new database connection.
    /// The connection is not opened automatically - call OpenAsync() before use.
    /// </summary>
    /// <returns>A new database connection.</returns>
    DbConnection CreateConnection();
}
