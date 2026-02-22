using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Propentro.Domain.Common;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.SyncUser;

/// <summary>
/// Upserts a user by (external_id, auth_provider). Idempotent.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncUserHandler(IAppDbContext dbContext) : ICommandHandler<SyncUserCommand>
{
    public async Task<Result> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            user = new Domain.Entities.User
            {
                Id = Guid.CreateVersion7(),
                PublicId = PublicIdGenerator.Generate("usr"),
                ExternalId = request.ExternalId,
                AuthProvider = request.AuthProvider,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            dbContext.Users.Add(user);
            Log.Information("Created user {PublicId} for external ID {ExternalId}",
                user.PublicId, request.ExternalId);
        }
        else
        {
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.IsActive = true;

            Log.Information("Updated user {PublicId} for external ID {ExternalId}",
                user.PublicId, request.ExternalId);
        }

        return Result.Success();
    }
}
