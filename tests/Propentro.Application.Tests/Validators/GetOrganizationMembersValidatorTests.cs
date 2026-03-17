using FluentValidation.TestHelper;
using Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;

namespace Propentro.Application.Tests.Validators;

public class GetOrganizationMembersValidatorTests
{
    private readonly GetOrganizationMembersValidator _validator = new();

    private static GetOrganizationMembersQuery CreateValidQuery() =>
        new(Page: 1, PageSize: 25, Search: null, Status: null, RoleId: null);

    [Fact]
    public void Valid_Query_Should_Pass()
    {
        var result = _validator.TestValidate(CreateValidQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("active")]
    [InlineData("deactivated")]
    [InlineData(null)]
    public void Valid_Status_Should_Pass(string? status)
    {
        var query = CreateValidQuery() with { Status = status };
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("pending")]
    [InlineData("ACTIVE")]
    public void Invalid_Status_Should_Fail(string status)
    {
        var query = CreateValidQuery() with { Status = status };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("rol_a1b2c3d4e5f6")]
    [InlineData(null)]
    public void Valid_RoleId_Should_Pass(string? roleId)
    {
        var query = CreateValidQuery() with { RoleId = roleId };
        var result = _validator.TestValidate(query);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("rol_short")]
    [InlineData("usr_a1b2c3d4e5f6")]
    public void Invalid_RoleId_Should_Fail(string roleId)
    {
        var query = CreateValidQuery() with { RoleId = roleId };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }

    [Fact]
    public void Page_Zero_Should_Fail()
    {
        var query = CreateValidQuery() with { Page = 0 };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void PageSize_Over_100_Should_Fail()
    {
        var query = CreateValidQuery() with { PageSize = 101 };
        var result = _validator.TestValidate(query);
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }
}
