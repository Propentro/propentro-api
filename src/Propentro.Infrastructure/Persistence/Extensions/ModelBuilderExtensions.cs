using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void AddOrganizationFilter(this EntityTypeBuilder builder, ICurrentUser currentUser)
    {
        var parameter = Expression.Parameter(builder.Metadata.ClrType, "e");
        var property = Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationId));
        var tenantId = Expression.Property(
            Expression.Constant(currentUser),
            nameof(ICurrentUser.OrganizationId));

        var filter = Expression.Lambda(Expression.Equal(property, tenantId), parameter);
        builder.HasQueryFilter(filter);
    }

    public static void AddSoftDeleteFilter(this EntityTypeBuilder builder)
    {
        var parameter = Expression.Parameter(builder.Metadata.ClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
        var filter = Expression.Lambda(
            Expression.Equal(property, Expression.Constant(true)),
            parameter);

        builder.HasQueryFilter(filter);
    }
}
