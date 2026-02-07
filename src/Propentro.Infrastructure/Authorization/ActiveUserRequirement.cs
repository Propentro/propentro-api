using Microsoft.AspNetCore.Authorization;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement that ensures the authenticated user has been resolved
/// to an active internal identity (valid UserId and OrganizationId).
/// Added to the default authorization policy so ALL authorized endpoints enforce this.
/// </summary>
public sealed class ActiveUserRequirement : IAuthorizationRequirement;
