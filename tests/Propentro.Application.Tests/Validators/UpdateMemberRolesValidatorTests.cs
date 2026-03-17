using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

namespace Propentro.Application.Tests.Validators;

public class UpdateMemberRolesValidatorTests
{
    private readonly UpdateMemberRolesValidator _validator = new();

    private static UpdateMemberRolesCommand CreateValidCommand() =>
        new("usr_a1b2c3d4e5f6", ["rol_a1b2c3d4e5f6"]);

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("usr_short")]
    public void Invalid_UserId_Format_Should_Fail(string userId)
    {
        var command = CreateValidCommand() with { UserId = userId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Empty_RoleIds_Should_Fail()
    {
        var command = CreateValidCommand() with { RoleIds = [] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }

    [Fact]
    public void More_Than_20_RoleIds_Should_Fail()
    {
        var roleIds = Enumerable.Range(0, 21)
            .Select(i => $"rol_{i:x12}")
            .ToArray();
        var command = CreateValidCommand() with { RoleIds = roleIds };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }

    [Fact]
    public void Invalid_RoleId_Format_Should_Fail()
    {
        var command = CreateValidCommand() with { RoleIds = ["not_a_role_id"] };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleIds);
    }
}
