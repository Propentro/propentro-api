using Propentro.Application.Common.Models;
using Propentro.Contracts.Activities;

namespace Propentro.Application.Features.Activities.Queries.GetActivities;

public sealed record GetActivitiesQuery(
    string ContactId,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<ActivityResponse>>;
