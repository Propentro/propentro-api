using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations;

public static class AdminOrganizationErrors
{
    public static readonly Error SlugTaken = Error.Conflict(
        "Organization.SlugTaken", "An organization with this slug already exists.");

    public static readonly Error AlreadyInactive = Error.Conflict(
        "Organization.AlreadyInactive", "Organization is already inactive.");

    public static readonly Error AlreadyActive = Error.Conflict(
        "Organization.AlreadyActive", "Organization is already active.");
}
