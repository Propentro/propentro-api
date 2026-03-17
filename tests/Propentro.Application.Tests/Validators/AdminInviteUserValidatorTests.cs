using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

namespace Propentro.Application.Tests.Validators;

public class AdminInviteUserValidatorTests
{
    private readonly InviteUserValidator _validator = new();

    private static InviteUserCommand CreateValidCommand() =>
        new("org_a1b2c3d4e5f6", "user@example.com", "rol_a1b2c3d4e5f6", Guid.NewGuid());

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_OrganizationId_Should_Fail(string? orgId)
    {
        var command = CreateValidCommand() with { OrganizationId = orgId! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
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
