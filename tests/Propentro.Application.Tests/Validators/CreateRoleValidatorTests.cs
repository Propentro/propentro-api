using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;

namespace Propentro.Application.Tests.Validators;

public class CreateRoleValidatorTests
{
    private readonly CreateRoleValidator _validator = new();

    private static CreateRoleCommand CreateValidCommand() =>
        new(
            Name: "Manager",
            Description: "Manages the team",
            PermissionCodes: ["properties.read", "properties.write"]);

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Name_Should_Fail(string? name)
    {
        var command = CreateValidCommand() with { Name = name! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Exceeding_Max_Length_Should_Fail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 101) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Null_PermissionCodes_Should_Fail()
    {
        var command = CreateValidCommand() with { PermissionCodes = null! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PermissionCodes);
    }

    [Fact]
    public void Empty_PermissionCodes_Should_Pass()
    {
        var command = CreateValidCommand() with { PermissionCodes = [] };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.PermissionCodes);
    }

    [Fact]
    public void Null_Description_Should_Pass()
    {
        var command = CreateValidCommand() with { Description = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
