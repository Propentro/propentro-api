using FluentValidation;

namespace Propentro.Application.Properties.Commands.CreateProperty;

public sealed class CreatePropertyValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Property name is required.")
            .MaximumLength(200).WithMessage("Property name cannot exceed 200 characters.");

        RuleFor(x => x.PropertyKind)
            .NotEmpty()
            .Must(BeValidPropertyKind).WithMessage("Invalid property kind. Must be: standalone, building, complex, or land.");

        RuleFor(x => x.YearBuilt)
            .InclusiveBetween(1600, 2100).When(x => x.YearBuilt.HasValue)
            .WithMessage("Year built must be between 1600 and 2100.");

        RuleFor(x => x.TotalAreaSqm)
            .GreaterThan(0).When(x => x.TotalAreaSqm.HasValue)
            .WithMessage("Total area must be greater than 0.");

        RuleFor(x => x.TotalFloors)
            .InclusiveBetween(0, 300).When(x => x.TotalFloors.HasValue)
            .WithMessage("Total floors must be between 0 and 300.");

        RuleFor(x => x.ReferenceCode)
            .MaximumLength(50).When(x => !string.IsNullOrEmpty(x.ReferenceCode))
            .WithMessage("Reference code cannot exceed 50 characters.");
    }

    private static bool BeValidPropertyKind(string kind) =>
        kind is "standalone" or "building" or "complex" or "land";
}
