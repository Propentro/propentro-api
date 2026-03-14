using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Propentro.Application.Common.Interfaces.Authentication;

namespace Propentro.Infrastructure.Persistence;

/// <summary>
/// EF Core interceptor that sets PostgreSQL session variables for the audit trigger.
/// Runs SET LOCAL on each transaction so audit_logs captures who made the change.
/// SET LOCAL scopes to the current transaction only — safe for connection pooling.
/// </summary>
internal sealed class AuditContextInterceptor(IUserContext userContext) : DbTransactionInterceptor
{
    public override async ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        await SetAuditContextAsync(result, cancellationToken);
        return await base.TransactionStartedAsync(connection, eventData, result, cancellationToken);
    }

    public override DbTransaction TransactionStarted(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result)
    {
        SetAuditContext(result);
        return base.TransactionStarted(connection, eventData, result);
    }

    private async Task SetAuditContextAsync(DbTransaction transaction, CancellationToken ct)
    {
        if (!userContext.IsAuthenticated)
            return;

        await using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        SetParameterizedCommand(command);
        await command.ExecuteNonQueryAsync(ct);
    }

    private void SetAuditContext(DbTransaction transaction)
    {
        if (!userContext.IsAuthenticated)
            return;

        using var command = transaction.Connection!.CreateCommand();
        command.Transaction = transaction;
        SetParameterizedCommand(command);
        command.ExecuteNonQuery();
    }

    private void SetParameterizedCommand(DbCommand command)
    {
        // set_config(name, value, is_local=true) is equivalent to SET LOCAL but parameterizable
        command.CommandText = "SELECT set_config('app.current_user_id', @userId, true), set_config('app.source', 'api', true)";

        var param = command.CreateParameter();
        param.ParameterName = "userId";
        param.Value = userContext.UserId.ToString();
        command.Parameters.Add(param);
    }
}
