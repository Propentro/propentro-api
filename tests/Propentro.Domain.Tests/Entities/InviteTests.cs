using FluentAssertions;
using Propentro.Domain.Entities.Identity;

namespace Propentro.Domain.Tests.Entities;

public sealed class InviteTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static Invite CreateInvite(string status = Invite.Statuses.Pending, DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = Guid.NewGuid(),
            Email = "test@example.com",
            RoleId = Guid.NewGuid(),
            Status = status,
            ExpiresAt = expiresAt,
            CreatedAt = Now.AddDays(-1)
        };

    // --- IsExpired ---

    [Fact]
    public void IsExpired_Returns_True_When_Past_Expiry()
    {
        var invite = CreateInvite(expiresAt: Now.AddHours(-1));

        invite.IsExpired(Now).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_Returns_False_When_Before_Expiry()
    {
        var invite = CreateInvite(expiresAt: Now.AddHours(1));

        invite.IsExpired(Now).Should().BeFalse();
    }

    [Fact]
    public void IsExpired_Returns_False_When_ExpiresAt_Is_Null()
    {
        var invite = CreateInvite(expiresAt: null);

        invite.IsExpired(Now).Should().BeFalse();
    }

    // --- IsPendingAndValid ---

    [Fact]
    public void IsPendingAndValid_Returns_True_When_Pending_And_Not_Expired()
    {
        var invite = CreateInvite(
            status: Invite.Statuses.Pending,
            expiresAt: Now.AddHours(1));

        invite.IsPendingAndValid(Now).Should().BeTrue();
    }

    [Fact]
    public void IsPendingAndValid_Returns_True_When_Pending_And_No_Expiry()
    {
        var invite = CreateInvite(
            status: Invite.Statuses.Pending,
            expiresAt: null);

        invite.IsPendingAndValid(Now).Should().BeTrue();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Accepted()
    {
        var invite = CreateInvite(
            status: Invite.Statuses.Accepted,
            expiresAt: Now.AddHours(1));

        invite.IsPendingAndValid(Now).Should().BeFalse();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Pending_But_Expired()
    {
        var invite = CreateInvite(
            status: Invite.Statuses.Pending,
            expiresAt: Now.AddHours(-1));

        invite.IsPendingAndValid(Now).Should().BeFalse();
    }

    [Fact]
    public void IsPendingAndValid_Returns_False_When_Canceled()
    {
        var invite = CreateInvite(
            status: Invite.Statuses.Canceled,
            expiresAt: Now.AddHours(1));

        invite.IsPendingAndValid(Now).Should().BeFalse();
    }
}
