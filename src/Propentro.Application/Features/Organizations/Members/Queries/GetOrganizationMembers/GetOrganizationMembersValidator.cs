using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;

public sealed partial class GetOrganizationMembersValidator : AbstractValidator<GetOrganizationMembersQuery>
{
    public GetOrganizationMembersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Status)
            .Must(s => s is null
                || s == OrganizationUser.Statuses.Active
                || s == OrganizationUser.Statuses.Deactivated)
            .WithMessage($"Status must be '{OrganizationUser.Statuses.Active}' or '{OrganizationUser.Statuses.Deactivated}'.");

        RuleFor(x => x.RoleId)
            .Must(id => id is null || RoleIdRegex().IsMatch(id))
            .WithMessage("Invalid role ID format.");
    }

    [GeneratedRegex(@"^rol_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex RoleIdRegex();
}
