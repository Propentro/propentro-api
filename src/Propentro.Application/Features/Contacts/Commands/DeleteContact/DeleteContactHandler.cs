using Dapper;
using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed class DeleteContactHandler(
    IAppDbContext dbContext,
    IDbConnectionFactory db,
    ICurrentUser currentUser) : ICommandHandler<DeleteContactCommand>
{
    public async Task<Result> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string activeLeasesSql = """
            SELECT COUNT(1)
            FROM lease_contacts lc
            JOIN leases l ON l.id = lc.lease_id
            WHERE lc.contact_id = @ContactId
              AND l.organization_id = @OrganizationId
              AND l.is_active = true
              AND l.status IN ('active', 'draft')
            """;

        var activeLeaseCount = await connection.ExecuteScalarAsync<int>(
            activeLeasesSql,
            new { ContactId = contact.Id, currentUser.OrganizationId });

        if (activeLeaseCount > 0)
            return ContactErrors.HasActiveLeases;

        contact.IsActive = false;

        return Result.Success();
    }
}
