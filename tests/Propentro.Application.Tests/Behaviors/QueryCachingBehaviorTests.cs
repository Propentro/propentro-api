using FluentAssertions;
using NSubstitute;
using Propentro.Application.Common.Behaviors;
using Propentro.Application.Common.Interfaces.Caching;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Xunit;

namespace Propentro.Application.Tests.Behaviors;

public class QueryCachingBehaviorTests
{
    private readonly ICacheService _cacheService;
    private readonly QueryCachingBehavior<TestCachedQuery, Result<TestDto>> _behavior;

    public QueryCachingBehaviorTests()
    {
        _cacheService = Substitute.For<ICacheService>();
        _behavior = new QueryCachingBehavior<TestCachedQuery, Result<TestDto>>(_cacheService);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var query = new TestCachedQuery("test-key");
        var cachedDto = new TestDto("cached-value");

        _cacheService
            .GetAsync<TestDto>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns(cachedDto);

        var handlerCalled = false;
        Task<Result<TestDto>> Next(CancellationToken _)
        {
            handlerCalled = true;
            return Task.FromResult(Result.Success(new TestDto("fresh-value")));
        }

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(cachedDto);
        handlerCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldExecuteHandlerAndCache()
    {
        // Arrange
        var query = new TestCachedQuery("test-key");
        var freshDto = new TestDto("fresh-value");

        _cacheService
            .GetAsync<TestDto>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns((TestDto?)null);

        Task<Result<TestDto>> Next(CancellationToken _) => Task.FromResult(Result.Success(freshDto));

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(freshDto);

        await _cacheService.Received(1).SetAsync(
            query.CacheKey,
            freshDto,
            query.Expiration,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsFailure_ShouldNotCache()
    {
        // Arrange
        var query = new TestCachedQuery("test-key");
        var error = new Error("TEST_ERROR", "Test error", ErrorType.Validation);

        _cacheService
            .GetAsync<TestDto>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns((TestDto?)null);

        Task<Result<TestDto>> Next(CancellationToken _) => Task.FromResult(Result.Failure<TestDto>(error));

        // Act
        var result = await _behavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);

        await _cacheService.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<TestDto>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotCachedQuery_ShouldSkipCaching()
    {
        // Arrange
        var nonCachedBehavior = new QueryCachingBehavior<NonCachedQuery, Result<TestDto>>(_cacheService);
        var query = new NonCachedQuery("value");
        var freshDto = new TestDto("fresh-value");

        Task<Result<TestDto>> Next(CancellationToken _) => Task.FromResult(Result.Success(freshDto));

        // Act
        var result = await nonCachedBehavior.Handle(query, Next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(freshDto);

        // Cache should not be accessed for non-cached queries
        await _cacheService.DidNotReceive().GetAsync<TestDto>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    public sealed record TestCachedQuery(string Key) : IQuery<TestDto>, ICachedQuery<TestDto>
    {
        public string CacheKey => $"test:{Key}";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
    }

    public sealed record NonCachedQuery(string Value) : IQuery<TestDto>;

    public sealed record TestDto(string Value);
}
