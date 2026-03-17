using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberDeactivatedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberDeactivatedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.deactivated");
    }
}
