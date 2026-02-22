using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.DeactivateUser;

/// <summary>
/// Soft-deletes a user by (external_id, auth_provider). Idempotent.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class DeactivateUserHandler(IAppDbContext dbContext) : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            Log.Warning("Cannot deactivate user: no user found for external ID {ExternalId}",
                request.ExternalId);
            return Result.Success();
        }

        if (!user.IsActive)
        {
            Log.Debug("User {PublicId} is already deactivated", user.PublicId);
            return Result.Success();
        }

        user.IsActive = false;

        Log.Information("Deactivated user {PublicId} for external ID {ExternalId}",
            user.PublicId, request.ExternalId);

        return Result.Success();
    }
}
