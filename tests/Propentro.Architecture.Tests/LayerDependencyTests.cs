using FluentAssertions;
using NetArchTest.Rules;
using Propentro.Contracts.Me;
using Propentro.Domain.Common;

namespace Propentro.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly Types DomainTypes = Types.InAssembly(typeof(Entity).Assembly);
    private static readonly Types ContractsTypes = Types.InAssembly(typeof(MeResponse).Assembly);
    private static readonly Types ApplicationTypes = Types.InAssembly(typeof(Propentro.Application.DependencyInjection).Assembly);
    private static readonly Types InfrastructureTypes = Types.InAssembly(typeof(Propentro.Infrastructure.DependencyInjection).Assembly);
    private static readonly Types ApiTypes = Types.InAssembly(typeof(Program).Assembly);

    // Domain — cannot reference Application, Infrastructure, Api, Contracts
    [Fact]
    public void Domain_ShouldNot_DependOn_Application()
    {
        var result = DomainTypes.ShouldNot().HaveDependencyOn("Propentro.Application").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Infrastructure()
    {
        var result = DomainTypes.ShouldNot().HaveDependencyOn("Propentro.Infrastructure").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Api()
    {
        var result = DomainTypes.ShouldNot().HaveDependencyOn("Propentro.Api").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Domain_ShouldNot_DependOn_Contracts()
    {
        var result = DomainTypes.ShouldNot().HaveDependencyOn("Propentro.Contracts").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    // Contracts — cannot reference Domain, Application, Infrastructure, Api
    [Fact]
    public void Contracts_ShouldNot_DependOn_Domain()
    {
        var result = ContractsTypes.ShouldNot().HaveDependencyOn("Propentro.Domain").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Contracts_ShouldNot_DependOn_Application()
    {
        var result = ContractsTypes.ShouldNot().HaveDependencyOn("Propentro.Application").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Contracts_ShouldNot_DependOn_Infrastructure()
    {
        var result = ContractsTypes.ShouldNot().HaveDependencyOn("Propentro.Infrastructure").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Contracts_ShouldNot_DependOn_Api()
    {
        var result = ContractsTypes.ShouldNot().HaveDependencyOn("Propentro.Api").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    // Application — cannot reference Infrastructure, Api
    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure()
    {
        var result = ApplicationTypes.ShouldNot().HaveDependencyOn("Propentro.Infrastructure").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Api()
    {
        var result = ApplicationTypes.ShouldNot().HaveDependencyOn("Propentro.Api").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    // Infrastructure — cannot reference Api, Contracts
    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Api()
    {
        var result = InfrastructureTypes.ShouldNot().HaveDependencyOn("Propentro.Api").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }

    [Fact]
    public void Infrastructure_ShouldNot_DependOn_Contracts()
    {
        var result = InfrastructureTypes.ShouldNot().HaveDependencyOn("Propentro.Contracts").GetResult();
        result.IsSuccessful.Should().BeTrue(because: result.FailingTypeNames is { } names ? string.Join(", ", names) : string.Empty);
    }
}
