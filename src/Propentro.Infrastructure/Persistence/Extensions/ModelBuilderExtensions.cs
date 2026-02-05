using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for configuring global query filters on entity types.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds a combined filter for organization scoping and soft delete.
    /// Combines both filters into a single expression to avoid EF Core override issue.
    /// </summary>
    public static void AddGlobalFilters(
        this EntityTypeBuilder builder,
        ICurrentUser currentUser,
        bool isOrganizationScoped,
        bool isSoftDeletable)
    {
        var parameter = Expression.Parameter(builder.Metadata.ClrType, "e");
        Expression? combinedFilter = null;

        if (isOrganizationScoped)
        {
            var orgProperty = Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationId));
            var organizationId = Expression.Property(
                Expression.Constant(currentUser),
                nameof(ICurrentUser.OrganizationId));

            combinedFilter = Expression.Equal(orgProperty, organizationId);
        }

        if (isSoftDeletable)
        {
            var isActiveProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
            var softDeleteFilter = Expression.Equal(isActiveProperty, Expression.Constant(true));

            combinedFilter = combinedFilter is not null
                ? Expression.AndAlso(combinedFilter, softDeleteFilter)
                : softDeleteFilter;
        }

        if (combinedFilter is not null)
        {
            builder.HasQueryFilter(Expression.Lambda(combinedFilter, parameter));
        }
    }
}
