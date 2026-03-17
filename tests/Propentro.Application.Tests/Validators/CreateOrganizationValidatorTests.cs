using FluentValidation.TestHelper;
using Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

namespace Propentro.Application.Tests.Validators;

public class CreateOrganizationValidatorTests
{
    private readonly CreateOrganizationValidator _validator = new();

    private static CreateOrganizationCommand CreateValidCommand() =>
        new("Test Org", "test-org", "Contact", "contact@example.com", "EUR", "UTC", "starter");

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

    [Theory]
    [InlineData("invalid")]
    [InlineData("free")]
    public void Invalid_SubscriptionPlan_Should_Fail(string plan)
    {
        var command = CreateValidCommand() with { SubscriptionPlan = plan };
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionPlan);
    }

    [Theory]
    [InlineData("starter")]
    [InlineData("pro")]
    [InlineData("enterprise")]
    public void Valid_SubscriptionPlan_Should_Pass(string plan)
    {
        var command = CreateValidCommand() with { SubscriptionPlan = plan };
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.SubscriptionPlan);
    }
}
