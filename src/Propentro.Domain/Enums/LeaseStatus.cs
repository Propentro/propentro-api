namespace Propentro.Domain.Enums;

/// <summary>
/// Current status of a lease.
/// </summary>
public enum LeaseStatus
{
    Draft,
    Pending,
    Active,
    Expired,
    Terminated,
    Renewed
}
