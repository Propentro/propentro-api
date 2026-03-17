using FluentAssertions;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Messaging;
using Propentro.Domain.Common;

namespace Propentro.Application.Tests.Common.Messaging;

public class OutboxJsonOptionsTests
{
    [Fact]
    public void Deserialize_Should_Return_Object_From_Valid_Json()
    {
        var json = """{"name":"test","value":42}""";

        var result = OutboxJsonOptions.Deserialize<TestPayload>(json);

        result.Should().NotBeNull();
        result.Name.Should().Be("test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_Should_Handle_CamelCase_Properties()
    {
        var json = """{"firstName":"John","lastName":"Doe"}""";

        var result = OutboxJsonOptions.Deserialize<CamelCasePayload>(json);

        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Deserialize_Should_Throw_On_Null_Result()
    {
        var json = "null";

        var act = () => OutboxJsonOptions.Deserialize<TestPayload>(json);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestPayload*");
    }

    private sealed record TestPayload(string Name, int Value);
    private sealed record CamelCasePayload(string FirstName, string LastName);
}

public class OutboxEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_Should_Deserialize_Payload_And_Invoke_Typed_Handler()
    {
        var handler = new TestEventHandler();
        var payload = """{"aggregateId":"11111111-1111-1111-1111-111111111111","organizationId":"22222222-2222-2222-2222-222222222222"}""";

        await ((IOutboxEventHandler)handler).HandleAsync(payload);

        handler.ReceivedEvent.Should().NotBeNull();
        handler.ReceivedEvent!.AggregateId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        handler.ReceivedEvent.OrganizationId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public void EventType_Should_Match_Concrete_Handler()
    {
        var handler = new TestEventHandler();

        handler.EventType.Should().Be("test.event");
    }

    private sealed record TestEvent(Guid AggregateId, Guid OrganizationId)
        : DomainEvent(AggregateId, OrganizationId)
    {
        public override string EventType => "test.event";
    }

    private sealed class TestEventHandler : OutboxEventHandler<TestEvent>
    {
        public TestEvent? ReceivedEvent { get; private set; }

        public override string EventType => "test.event";

        protected override Task HandleAsync(TestEvent @event, CancellationToken cancellationToken)
        {
            ReceivedEvent = @event;
            return Task.CompletedTask;
        }
    }
}
