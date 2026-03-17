using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Admin.Me;

namespace Propentro.Application.Features.Admin.Me.Queries.GetAdminMe;

public sealed class GetAdminMeHandler(
    IDbConnectionFactory db,
    IUserContext userContext) : IQueryHandler<GetAdminMeQuery, AdminMeResponse>
{
    public async Task<Result<AdminMeResponse>> Handle(
        GetAdminMeQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                u.public_id AS user_id,
                u.email,
                u.first_name,
                u.last_name,
                sa.role AS admin_role,
                sa.created_at AS assigned_at
            FROM system_administrators sa
            JOIN users u ON u.id = sa.user_id
            WHERE sa.user_id = @UserId
              AND sa.is_active = true
              AND u.is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<AdminMeResponse>(
            sql, new { userContext.UserId });

        if (result is null)
            return Error.NotFound("Admin.NotFound", "Admin profile not found.");

        return result;
    }
}
