using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

namespace Propentro.Application.Tests.Validators;

public class CreateOrganizationValidatorTests
{
    private readonly CreateOrganizationValidator _validator = new();

    private static CreateOrganizationCommand CreateValidCommand() =>
        new("Test Org", "test-org", "EUR", "UTC");

    [Fact]
    public void Valid_Command_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidCommand());
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
    public void Name_Exceeding_100_Chars_Should_Fail()
    {
        var command = CreateValidCommand() with { Name = new string('A', 101) };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("UPPER-CASE")]
    [InlineData("has spaces")]
    [InlineData("special!chars")]
    public void Invalid_Slug_Should_Fail(string? slug)
    {
        var command = CreateValidCommand() with { Slug = slug! };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Slug);
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
