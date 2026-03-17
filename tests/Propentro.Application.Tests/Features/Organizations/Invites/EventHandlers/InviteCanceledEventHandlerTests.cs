using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Invites.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Invites.EventHandlers;

public class InviteCanceledEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_InviteCanceled()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new InviteCanceledEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("invite.canceled");
    }
}
