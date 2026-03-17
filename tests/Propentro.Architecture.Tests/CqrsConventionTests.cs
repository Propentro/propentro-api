using FluentAssertions;
using NetArchTest.Rules;

namespace Propentro.Architecture.Tests;

public class CqrsConventionTests
{
    private static readonly Types ApplicationTypes =
        Types.InAssembly(typeof(Propentro.Application.DependencyInjection).Assembly);

    // ── Command handlers must not use Dapper ─────────────────────────────────

    [Fact]
    public void CommandHandlers_ShouldNot_UseDapper()
    {
        var result = ApplicationTypes
            .That()
            .ResideInNamespaceMatching(@".*\.Commands\..*")
            .And()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .ShouldNot()
            .HaveDependencyOn("Dapper")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : string.Empty);
    }

    // ── Query handlers must not use EF Core ──────────────────────────────────

    [Fact]
    public void QueryHandlers_ShouldNot_UseEfCore()
    {
        var result = ApplicationTypes
            .That()
            .ResideInNamespaceMatching(@".*\.Queries\..*")
            .And()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : string.Empty);
    }

    // ── Command records (ICommand / ICommand<T>) must be sealed ──────────────

    [Fact]
    public void CommandRecords_ShouldBe_Sealed()
    {
        var result = ApplicationTypes
            .That()
            .ResideInNamespaceMatching(@".*\.Commands\..*")
            .And()
            .HaveNameEndingWith("Command")
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : string.Empty);
    }

    // ── Query records (IQuery<T>) must be sealed ──────────────────────────────

    [Fact]
    public void QueryRecords_ShouldBe_Sealed()
    {
        var sealedResult = ApplicationTypes
            .That()
            .ResideInNamespaceMatching(@".*\.Queries\..*")
            .And()
            .HaveNameEndingWith("Query")
            .Should()
            .BeSealed()
            .GetResult();

        sealedResult.IsSuccessful.Should().BeTrue(
            because: sealedResult.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : string.Empty);
    }

    // ── Handler classes (non-abstract) must be sealed ────────────────────────

    [Fact]
    public void HandlerClasses_ShouldBe_Sealed()
    {
        var result = ApplicationTypes
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: result.FailingTypeNames is { } names
                ? string.Join(", ", names)
                : string.Empty);
    }
}
