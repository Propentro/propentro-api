using FluentAssertions;
using MediatR;
using NSubstitute;
using Propentro.Application.Common.Behaviors;
using Propentro.Application.Common.Interfaces.Identity;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Xunit;

namespace Propentro.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ICurrentUser _currentUser;
    private readonly LoggingBehavior<TestCommand, Result<string>> _behavior;

    public LoggingBehaviorTests()
    {
        _currentUser = Substitute.For<ICurrentUser, ICurrentUserInitializer>();
        _behavior = new LoggingBehavior<TestCommand, Result<string>>(_currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserIsAuthenticated_ShouldExecuteAndReturnResponse()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.OrganizationId.Returns(Guid.NewGuid());
        ((ICurrentUserInitializer)_currentUser).IsInitialized.Returns(true);

        var request = new TestCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ShouldStillExecute()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        ((ICurrentUserInitializer)_currentUser).IsInitialized.Returns(true);

        var request = new TestCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenUserContextNotInitialized_ShouldStillExecute()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        ((ICurrentUserInitializer)_currentUser).IsInitialized.Returns(false);

        var request = new TestCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldRethrowException()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        ((ICurrentUserInitializer)_currentUser).IsInitialized.Returns(true);

        var request = new TestCommand("test");
        var expectedException = new InvalidOperationException("Test exception");

        Task<Result<string>> Next(CancellationToken _) => throw expectedException;

        // Act
        var act = async () => await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    public sealed record TestCommand(string Value) : ICommand<string>;
}
