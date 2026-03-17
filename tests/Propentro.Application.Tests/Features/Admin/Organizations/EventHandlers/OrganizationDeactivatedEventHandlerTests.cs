using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Admin.Organizations.EventHandlers;

namespace Propentro.Application.Tests.Features.Admin.Organizations.EventHandlers;

public class OrganizationDeactivatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_OrganizationDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new OrganizationDeactivatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("organization.deactivated");
    }
}
