using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Admin.Organizations.EventHandlers;
using Propentro.Application.Tests.Shared;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Tests.Features.Admin.Organizations.EventHandlers;

public class OrganizationDeactivatedEventHandlerTests : IDisposable
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IEmailTemplateService _templateService = Substitute.For<IEmailTemplateService>();
    private readonly TestDbHelper _testDb = new();

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

    [Fact]
    public async Task HandleAsync_Sends_Email_To_All_Members()
    {
        var orgId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        _testDb.InsertOrganization(orgId, "Acme Corp");
        _testDb.InsertUser(userId1, "alice@test.com", "Alice");
        _testDb.InsertUser(userId2, "bob@test.com", "Bob");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email1 = new EmailMessage("alice@test.com", "Subject", "<p>Body</p>");
        var email2 = new EmailMessage("bob@test.com", "Subject", "<p>Body</p>");
        _templateService.OrganizationDeactivated("alice@test.com", "Alice", "Acme Corp").Returns(email1);
        _templateService.OrganizationDeactivated("bob@test.com", "Bob", "Acme Corp").Returns(email2);
        _emailService.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, [userId1, userId2]);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        _templateService.Received(1).OrganizationDeactivated("alice@test.com", "Alice", "Acme Corp");
        _templateService.Received(1).OrganizationDeactivated("bob@test.com", "Bob", "Acme Corp");
        await _emailService.Received(2).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Returns_Early_When_Org_Not_Found()
    {
        var userId = Guid.NewGuid();
        _testDb.InsertUser(userId, "user@test.com");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var orgId = Guid.NewGuid();
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, [userId]);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        await _emailService.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Throws_When_Email_Send_Fails()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _testDb.InsertOrganization(orgId, "Acme Corp");
        _testDb.InsertUser(userId, "user@test.com", "Alice");

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.OrganizationDeactivated(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Email.SendFailed", "Send failed", ErrorType.ExternalService)));

        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, [userId]);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(payload, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Uses_There_When_Name_Is_Null()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _testDb.InsertOrganization(orgId, "Acme Corp");
        _testDb.InsertUser(userId, "user@test.com", null);

        var db = Substitute.For<IDbConnectionFactory>();
        db.CreateConnection().Returns(_testDb.CreateHandlerConnection());

        var email = new EmailMessage("user@test.com", "Subject", "<p>Body</p>");
        _templateService.OrganizationDeactivated("user@test.com", "there", "Acme Corp").Returns(email);
        _emailService.SendAsync(email, Arg.Any<CancellationToken>()).Returns(Result.Success());

        var handler = new OrganizationDeactivatedEventHandler(db, _emailService, _templateService);
        var @event = new OrganizationDeactivatedEvent(orgId, orgId, [userId]);
        var payload = JsonSerializer.Serialize(@event, OutboxJsonOptions.Instance);

        await handler.HandleAsync(payload, CancellationToken.None);

        _templateService.Received(1).OrganizationDeactivated("user@test.com", "there", "Acme Corp");
    }

    public void Dispose()
    {
        _testDb.Dispose();
        GC.SuppressFinalize(this);
    }
}
