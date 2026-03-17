using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

namespace Propentro.Application.Tests.Validators;

public class InviteToCurrentOrgValidatorTests
{
    private readonly InviteToCurrentOrgValidator _validator = new();

    private static InviteToCurrentOrgCommand CreateValidCommand() =>
        new("user@example.com", "rol_a1b2c3d4e5f6", Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("not-an-email")]
    public void Invalid_Email_Should_Fail(string? email)
    {
        var command = CreateValidCommand() with { Email = email! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_RoleId_Should_Fail(string? roleId)
    {
        var command = CreateValidCommand() with { RoleId = roleId! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
