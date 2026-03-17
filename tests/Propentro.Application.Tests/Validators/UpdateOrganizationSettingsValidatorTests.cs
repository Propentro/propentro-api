using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Settings.Commands.UpdateOrganizationSettings;

namespace Propentro.Application.Tests.Validators;

public class UpdateOrganizationSettingsValidatorTests
{
    private readonly UpdateOrganizationSettingsValidator _validator = new();

    private static UpdateOrganizationSettingsCommand CreateValidCommand() =>
        new(
            Name: "Acme Corp",
            ContactName: "John Doe",
            ContactEmail: "john@acme.com",
            ContactPhone: "+1234567890",
            AddressLine1: "123 Main St",
            AddressLine2: null,
            City: "New York",
            Region: "NY",
            PostalCode: "10001",
            CountryCode: "US",
            LegalName: "Acme Corporation LLC",
            TaxNumber: "123456789",
            TaxCountry: "United States",
            CurrencyCode: "USD",
            Timezone: "America/New_York");

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

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@missing.com")]
    public void Invalid_ContactEmail_Should_Fail(string email)
    {
        var command = CreateValidCommand() with { ContactEmail = email };

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
    [InlineData("U")]
    [InlineData("USA")]
    public void Invalid_CountryCode_Length_Should_Fail(string countryCode)
    {
        var command = CreateValidCommand() with { CountryCode = countryCode };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CountryCode);
    }

    [Fact]
    public void Null_CountryCode_Should_Pass()
    {
        var command = CreateValidCommand() with { CountryCode = null };

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.CountryCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_CurrencyCode_Should_Fail(string? currencyCode)
    {
        var command = CreateValidCommand() with { CurrencyCode = currencyCode! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Invalid_CurrencyCode_Length_Should_Fail(string currencyCode)
    {
        var command = CreateValidCommand() with { CurrencyCode = currencyCode };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Empty_Or_Null_Timezone_Should_Fail(string? timezone)
    {
        var command = CreateValidCommand() with { Timezone = timezone! };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Timezone);
    }

    [Fact]
    public void ContactPhone_Exceeding_Max_Length_Should_Fail()
    {
        var command = CreateValidCommand() with { ContactPhone = new string('1', 51) };

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ContactPhone);
    }
}
