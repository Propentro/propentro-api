using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Application.Features.Organizations.Members.Queries.GetMember;

public sealed class GetMemberHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetMemberQuery, MemberListItemResponse>
{
    public async Task<Result<MemberListItemResponse>> Handle(
        GetMemberQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string memberSql = """
            SELECT
                u.public_id AS user_id,
                u.email,
                u.first_name,
                u.last_name,
                ou.status,
                ou.created_at AS joined_at,
                u.id AS user_internal_id
            FROM organization_users ou
            JOIN users u ON u.id = ou.user_id
            WHERE ou.organization_id = @OrganizationId
              AND u.public_id = @UserId
            """;

        var member = await connection.QueryFirstOrDefaultAsync<MemberQueryResult>(
            memberSql, new { currentUser.OrganizationId, request.UserId });

        if (member is null)
            return MemberErrors.NotFound;

        const string rolesSql = """
            SELECT
                r.public_id AS id,
                r.name,
                r.is_system
            FROM organization_user_roles our
            JOIN roles r ON r.id = our.role_id
            WHERE our.organization_id = @OrganizationId
              AND our.user_id = @UserInternalId
              AND r.is_active = true
            """;

        var roles = (await connection.QueryAsync<MemberRoleResponse>(
            rolesSql, new { currentUser.OrganizationId, member.UserInternalId })).ToList();

        return new MemberListItemResponse(
            member.UserId,
            member.Email,
            member.FirstName,
            member.LastName,
            member.Status,
            member.JoinedAt,
            roles);
    }

    private sealed class MemberQueryResult
    {
        public string UserId { get; init; } = null!;
        public string Email { get; init; } = null!;
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string Status { get; init; } = null!;
        public DateTimeOffset JoinedAt { get; init; }
        public Guid UserInternalId { get; init; }
    }
}
