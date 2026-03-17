using FluentAssertions;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Infrastructure.Email;

namespace Propentro.Infrastructure.Tests.Email;

public class EmailTemplateServiceTests
{
    private readonly IEmailTemplateService _sut = new EmailTemplateService();

    [Fact]
    public void MemberDeactivated_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberDeactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("John");
        result.HtmlBody.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("deactivated");
    }

    [Fact]
    public void MemberReactivated_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberReactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("reactivated");
    }

    [Fact]
    public void MemberRemoved_Should_Return_Email_With_Correct_Subject_And_Recipient()
    {
        var result = _sut.MemberRemoved("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
        result.HtmlBody.Should().Contain("removed");
    }

    [Fact]
    public void MemberRolesUpdated_Should_Include_Role_Names()
    {
        var result = _sut.MemberRolesUpdated("john@example.com", "John", "Acme Corp", ["Owner", "Manager"]);

        result.To.Should().Be("john@example.com");
        result.HtmlBody.Should().Contain("Owner");
        result.HtmlBody.Should().Contain("Manager");
    }

    [Fact]
    public void InviteAccepted_Should_Return_Welcome_Email()
    {
        var result = _sut.InviteAccepted("john@example.com", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
    }

    [Fact]
    public void InviteCanceled_Should_Return_Email()
    {
        var result = _sut.InviteCanceled("john@example.com", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.Subject.Should().Contain("Acme Corp");
    }

    [Fact]
    public void OrganizationDeactivated_Should_Return_Email()
    {
        var result = _sut.OrganizationDeactivated("john@example.com", "John", "Acme Corp");

        result.To.Should().Be("john@example.com");
        result.HtmlBody.Should().Contain("deactivated");
        result.HtmlBody.Should().Contain("Acme Corp");
    }
}
