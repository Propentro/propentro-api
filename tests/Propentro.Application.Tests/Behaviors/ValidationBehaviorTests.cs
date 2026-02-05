using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Propentro.Application.Common.Behaviors;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Xunit;

namespace Propentro.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_ShouldExecuteHandler()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestValidationCommand>>();
        var behavior = new ValidationBehavior<TestValidationCommand, Result<string>>(validators);
        var command = new TestValidationCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldExecuteHandler()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationCommand>>();
        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestValidationCommand, Result<string>>(new[] { validator });
        var command = new TestValidationCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestValidationCommand>>();
        var failures = new List<ValidationFailure>
        {
            new("Property1", "Error message 1"),
            new("Property2", "Error message 2")
        };

        validator
            .ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestValidationCommand, Result<string>>(new[] { validator });
        var command = new TestValidationCommand("test");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(Result.Success("should not reach"));

        // Act
        var act = async () => await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldRunAllValidators()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestValidationCommand>>();
        var validator2 = Substitute.For<IValidator<TestValidationCommand>>();

        validator1
            .ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        validator2
            .ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestValidationCommand, Result<string>>(new[] { validator1, validator2 });
        var command = new TestValidationCommand("test");
        var expectedResult = Result.Success("success");

        Task<Result<string>> Next(CancellationToken _) => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(command, Next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        await validator1.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>());
        await validator2.Received(1).ValidateAsync(Arg.Any<ValidationContext<TestValidationCommand>>(), Arg.Any<CancellationToken>());
    }

    public sealed record TestValidationCommand(string Value) : ICommand<string>;
}
