using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public sealed partial class GetActivitiesValidator : AbstractValidator<GetActivitiesQuery>
{
    public GetActivitiesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.EntityType)
            .NotEmpty()
            .Must(t => t == "contact")
            .WithMessage("Entity type must be 'contact'.");

        RuleFor(x => x.EntityId)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .When(x => x.EntityType == "contact")
            .WithMessage("Invalid contact ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
