using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

namespace Propentro.Application.Tests.Validators;

public class AdminDeactivateMemberValidatorTests
{
    private readonly AdminDeactivateMemberValidator _validator = new();

    private static AdminDeactivateMemberCommand CreateValidCommand() =>
        new(OrganizationId: "org_a1b2c3d4e5f6", UserId: "usr_a1b2c3d4e5f6");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("usr_a1b2c3d4e5f6")]
    [InlineData("org_short")]
    public void Invalid_OrganizationId_Should_Fail(string orgId)
    {
        var command = CreateValidCommand() with { OrganizationId = orgId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("org_a1b2c3d4e5f6")]
    [InlineData("usr_short")]
    public void Invalid_UserId_Should_Fail(string userId)
    {
        var command = CreateValidCommand() with { UserId = userId };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
}
