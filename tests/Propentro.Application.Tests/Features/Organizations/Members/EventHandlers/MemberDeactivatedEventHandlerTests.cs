using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members.EventHandlers;
using Propentro.Application.Tests.Shared;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Organizations.Members.EventHandlers;

public class MemberDeactivatedEventHandlerTests : IDisposable
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();
    private readonly TestDbHelper _testDb = new();

    [Fact]
    public void EventType_Should_Be_MemberDeactivated()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);

        handler.EventType.Should().Be("member.deactivated");
    }

    [Fact]
    public async Task HandleAsync_Sends_Email_When_User_And_Org_Found()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var expectedEmail = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated("user@test.com", "Alice", "Acme Corp").Returns(expectedEmail);
        _emailService.SendAsync(expectedEmail, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        _templateService.Received(1).MemberDeactivated("user@test.com", "Alice", "Acme Corp");
        await _emailService.Received(1).SendAsync(expectedEmail, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_User_Not_Found()
    {
        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(Guid.NewGuid(), Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_Org_Not_Found()
    {
        var userId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, Guid.NewGuid());
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_When_Email_Send_Fails()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", "Alice");
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Email.SendFailed", "Send failed", ErrorType.ExternalService)));

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(payload, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Uses_There_When_Name_Is_Null()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com", null);
        _testDb.InsertOrganization(orgId, "Acme Corp");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.MemberDeactivated("user@test.com", "there", "Acme Corp").Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new MemberDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new MemberDeactivatedEvent(userId, orgId);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        _templateService.Received(1).MemberDeactivated("user@test.com", "there", "Acme Corp");
    }

    public void Dispose()
    {
        _testDb.Dispose();
        GC.SuppressFinalize(this);
    }
}
