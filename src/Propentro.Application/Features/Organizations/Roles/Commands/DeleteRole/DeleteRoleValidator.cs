using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed partial class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .Must(id => RoleIdRegex().IsMatch(id))
            .WithMessage("Invalid role ID format.");
    }

    [GeneratedRegex(@"^rol_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex RoleIdRegex();
}
