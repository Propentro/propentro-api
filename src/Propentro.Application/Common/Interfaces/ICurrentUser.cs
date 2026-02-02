namespace Propentro.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? UserId { get; }
    Guid OrganizationId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
