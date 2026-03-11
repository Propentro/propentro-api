using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;

public sealed partial class UpdateRoleValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty()
            .Must(id => RoleIdRegex().IsMatch(id))
            .WithMessage("Invalid role ID format.");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name is not null);
    }

    [GeneratedRegex(@"^rol_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex RoleIdRegex();
}
