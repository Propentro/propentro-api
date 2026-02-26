using Propentro.Application.Common.Models;
using Propentro.Contracts.Activities;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public record GetActivitiesQuery(
    string EntityType,
    string EntityId,
    int Page,
    int PageSize) : IQuery<PagedResult<ActivityResponse>>;
