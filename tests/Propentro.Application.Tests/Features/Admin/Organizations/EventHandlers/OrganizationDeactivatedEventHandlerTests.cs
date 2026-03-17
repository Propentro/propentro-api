using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Features.Admin.Organizations.EventHandlers;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Admin.Organizations.EventHandlers;

public class OrganizationDeactivatedEventHandlerTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();

    [Fact]
    public void EventType_Should_Be_OrganizationDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);

        handler.EventType.Should().Be("organization.deactivated");
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_No_Members()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var orgId = Guid.NewGuid();
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, []);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        db.DidNotReceive().CreateConnection();
        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
