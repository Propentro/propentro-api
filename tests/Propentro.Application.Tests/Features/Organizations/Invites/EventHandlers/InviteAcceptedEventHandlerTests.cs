using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Invites.EventHandlers;
using Propentro.Application.Tests.Shared;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Organizations.Invites.EventHandlers;

public class InviteAcceptedEventHandlerTests : IDisposable
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();
    private readonly TestDbHelper _testDb = new();

    [Fact]
    public void EventType_Should_Be_InviteAccepted()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new InviteAcceptedEventHandler(db, _emailService, _templateService);

        handler.EventType.Should().Be("invite.accepted");
    }

    [Fact]
    public async Task HandleAsync_Sends_Email_When_Invite_And_Org_Found()
    {
        var inviteId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertInvite(inviteId, "invited@test.com");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var expectedEmail = new EmailMessage("invited@test.com", "Subject", "<p>Body</p>");
        _templateService.InviteAccepted("invited@test.com", "Acme Corp").Returns(expectedEmail);
        _emailService.SendAsync(expectedEmail, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new InviteAcceptedEventHandler(db, _emailService, _templateService);
        var @event = new InviteAcceptedEvent(inviteId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        _templateService.Received(1).InviteAccepted("invited@test.com", "Acme Corp");
        await _emailService.Received(1).SendAsync(expectedEmail, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_Invite_Not_Found()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new InviteAcceptedEventHandler(db, _emailService, _templateService);
        var @event = new InviteAcceptedEvent(Guid.NewGuid(), Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_Org_Not_Found()
    {
        var inviteId = Guid.NewGuid();
        _testDb.InsertInvite(inviteId, "invited@test.com");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new InviteAcceptedEventHandler(db, _emailService, _templateService);
        var @event = new InviteAcceptedEvent(inviteId, Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_When_Email_Send_Fails()
    {
        var inviteId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertInvite(inviteId, "invited@test.com");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("invited@test.com", "Subject", "<p>Body</p>");
        _templateService.InviteAccepted(Arg.Any<string>(), Arg.Any<string>()).Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Email.SendFailed", "Send failed", ErrorType.ExternalService)));

        var handler = new InviteAcceptedEventHandler(db, _emailService, _templateService);
        var @event = new InviteAcceptedEvent(inviteId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(payload, CancellationToken.None));
    }

    public void Dispose()
    {
        _testDb.Dispose();
        GC.SuppressFinalize(this);
    }
}
