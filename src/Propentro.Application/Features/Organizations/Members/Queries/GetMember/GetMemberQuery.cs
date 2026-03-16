using Propentro.Contracts.Organizations.Members;

namespace Propentro.Application.Features.Organizations.Members.Queries.GetMember;

public sealed record GetMemberQuery(string UserId) : IQuery<MemberListItemResponse>;
