using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Organizations.Members.EventHandlers;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberRemovedEventHandlerTests
{
    [Fact]
    public void EventType_Should_Be_MemberRemoved()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var emailService = Substitute.For<IEmailService>();
        var templateService = Substitute.For<IEmailTemplateService>();
        var handler = new MemberRemovedEventHandler(db, emailService, templateService);

        handler.EventType.Should().Be("member.removed");
    }
}
