namespace Propentro.Application.Common.Interfaces;

public interface IPermissionService
{
    Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, Guid organizationId, CancellationToken ct = default);
}
