using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for configuring global query filters on entity types.
/// These filters are automatically applied to all queries unless explicitly ignored.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Adds a combined filter for organization scoping and soft delete.
    /// This method properly combines both filters into a single expression
    /// to avoid the EF Core issue where subsequent HasQueryFilter calls override previous ones.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="currentUser">The current user context for organization scoping.</param>
    /// <param name="isOrganizationScoped">Whether the entity implements IOrganizationScoped.</param>
    /// <param name="isSoftDeletable">Whether the entity implements ISoftDeletable.</param>
    public static void AddGlobalFilters(
        this EntityTypeBuilder builder,
        ICurrentUser currentUser,
        bool isOrganizationScoped,
        bool isSoftDeletable)
    {
        var parameter = Expression.Parameter(builder.Metadata.ClrType, "e");
        Expression? combinedFilter = null;

        // Organization scope filter: e.OrganizationId == currentUser.OrganizationId
        if (isOrganizationScoped)
        {
            var orgProperty = Expression.Property(parameter, nameof(IOrganizationScoped.OrganizationId));
            var tenantId = Expression.Property(
                Expression.Constant(currentUser),
                nameof(ICurrentUser.OrganizationId));

            combinedFilter = Expression.Equal(orgProperty, tenantId);
        }

        // Soft delete filter: e.IsActive == true
        if (isSoftDeletable)
        {
            var isActiveProperty = Expression.Property(parameter, nameof(ISoftDeletable.IsActive));
            var trueConstant = Expression.Constant(true);
            var softDeleteFilter = Expression.Equal(isActiveProperty, trueConstant);

            // Combine with AND if organization filter exists
            combinedFilter = combinedFilter is not null
                ? Expression.AndAlso(combinedFilter, softDeleteFilter)
                : softDeleteFilter;
        }

        // Apply the combined filter if any conditions exist
        if (combinedFilter is not null)
        {
            var lambda = Expression.Lambda(combinedFilter, parameter);
            builder.HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Adds only the organization scope filter.
    /// Use AddGlobalFilters instead when the entity also implements ISoftDeletable.
    /// </summary>
    [Obsolete("Use AddGlobalFilters to properly combine multiple filters.")]
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

    /// <summary>
    /// Adds only the soft delete filter.
    /// Use AddGlobalFilters to properly combine multiple filters.
    /// </summary>
    [Obsolete("Use AddGlobalFilters to properly combine multiple filters.")]
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
