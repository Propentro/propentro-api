using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

namespace Propentro.Application.Tests.Validators;

public class UpdateOrganizationValidatorTests
{
    private readonly UpdateOrganizationValidator _validator = new();

    private static UpdateOrganizationCommand CreateValidCommand() =>
        new("org_a1b2c3d4e5f6", "Updated Org", "updated-org",
            null, null, null, "EUR", "UTC",
            null, null, null, null, null, null, null, null, null);

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Id_Should_Fail(string? id)
    {
        var command = CreateValidCommand() with { Id = id! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
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
    public void Name_Exceeding_200_Chars_Should_Fail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 201) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Invalid_ContactEmail_Should_Fail()
    {
        var command = CreateValidCommand() with { ContactEmail = "not-an-email" };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Fact]
    public void Null_ContactEmail_Should_Pass()
    {
        var command = CreateValidCommand() with { ContactEmail = null };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.ContactEmail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("EURO")]
    public void Invalid_CurrencyCode_Should_Fail(string code)
    {
        var command = CreateValidCommand() with { CurrencyCode = code };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }
}
