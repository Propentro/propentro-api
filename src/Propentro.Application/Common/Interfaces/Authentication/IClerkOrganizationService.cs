using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Interfaces.Authentication;

public interface IClerkOrganizationService
{
    /// <summary>
    /// Syncs the organization name to Clerk. Called when name changes in settings.
    /// </summary>
    Task<Result> UpdateOrganizationAsync(string externalId, string name, CancellationToken cancellationToken);
}
