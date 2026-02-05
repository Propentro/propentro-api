using FluentAssertions;
using Propentro.Domain.Common;
using Xunit;

namespace Propentro.Domain.Tests;

public class EntityTests
{
    private sealed class TestEntity : Entity { }

    [Fact]
    public void Constructor_ShouldGenerateId()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_ShouldGeneratePublicId()
    {
        // Act
        var entity = new TestEntity();

        // Assert
        entity.PublicId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        // Act
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();

        // Assert
        entity1.Id.Should().NotBe(entity2.Id);
        entity1.PublicId.Should().NotBe(entity2.PublicId);
    }

    [Fact]
    public void PublicId_ShouldBeUrlSafe()
    {
        // Act
        var entity = new TestEntity();

        // Assert - PublicId should only contain URL-safe characters (alphanumeric)
        entity.PublicId.Should().MatchRegex(@"^[A-Za-z0-9]+$");
    }

    [Fact]
    public void PublicId_ShouldHaveReasonableLength()
    {
        // Act
        var entity = new TestEntity();

        // Assert - Base62 encoded 128-bit number should be ~22 chars max
        entity.PublicId.Length.Should().BeLessThanOrEqualTo(22);
        entity.PublicId.Length.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void Id_ShouldBeTimeOrdered()
    {
        // Act - create multiple entities with slight delay
        var entity1 = new TestEntity();
        var entity2 = new TestEntity();
        var entity3 = new TestEntity();

        // Convert to byte arrays and compare first 6 bytes (timestamp portion)
        var bytes1 = entity1.Id.ToByteArray();
        var bytes2 = entity2.Id.ToByteArray();
        var bytes3 = entity3.Id.ToByteArray();

        // Assert - entities created later should have greater timestamp bytes
        // Note: We compare byte arrays directly since the first 6 bytes are timestamp
        GetTimestampPortion(bytes1).Should().BeLessThanOrEqualTo(GetTimestampPortion(bytes2));
        GetTimestampPortion(bytes2).Should().BeLessThanOrEqualTo(GetTimestampPortion(bytes3));
    }

    private static long GetTimestampPortion(byte[] guidBytes)
    {
        // Extract the first 6 bytes as a big-endian long
        return ((long)guidBytes[0] << 40) |
               ((long)guidBytes[1] << 32) |
               ((long)guidBytes[2] << 24) |
               ((long)guidBytes[3] << 16) |
               ((long)guidBytes[4] << 8) |
               guidBytes[5];
    }
}
