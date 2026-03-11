using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

public sealed partial class UpdateMemberRolesValidator : AbstractValidator<UpdateMemberRolesCommand>
{
    public UpdateMemberRolesValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .Must(id => UserIdRegex().IsMatch(id))
            .WithMessage("Invalid user ID format.");

        RuleFor(x => x.RoleIds)
            .NotEmpty()
            .Must(ids => ids.Length <= 20)
            .WithMessage("Cannot assign more than 20 roles at once.")
            .Must(ids => ids.All(id => RoleIdRegex().IsMatch(id)))
            .WithMessage("One or more role IDs have an invalid format.");
    }

    [GeneratedRegex(@"^usr_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex UserIdRegex();

    [GeneratedRegex(@"^rol_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex RoleIdRegex();
}
