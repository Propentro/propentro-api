using System.Text.RegularExpressions;
using FluentAssertions;
using Propentro.Domain.Common;

namespace Propentro.Domain.Tests;

public sealed class PublicIdGeneratorTests
{
    [Fact]
    public void Generate_Returns_Prefixed_Id_With_Correct_Format()
    {
        var result = PublicIdGenerator.Generate("prp");

        result.Should().StartWith("prp_");
        result.Should().HaveLength("prp_".Length + 12);
    }

    [Fact]
    public void Generate_Returns_Unique_Ids()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => PublicIdGenerator.Generate("prp"))
            .ToHashSet();

        ids.Should().HaveCount(100);
    }

    [Fact]
    public void Generate_Returns_Lowercase_Hex()
    {
        var result = PublicIdGenerator.Generate("prp");
        var hex = result.Split('_')[1];

        hex.Should().MatchRegex(new Regex("^[0-9a-f]{12}$"));
    }

    [Theory]
    [InlineData("org")]
    [InlineData("usr")]
    [InlineData("inv")]
    [InlineData("prp")]
    public void Generate_Uses_Provided_Prefix(string prefix)
    {
        var result = PublicIdGenerator.Generate(prefix);

        result.Should().StartWith($"{prefix}_");
        var hex = result[(prefix.Length + 1)..];
        hex.Should().HaveLength(12);
    }
}
