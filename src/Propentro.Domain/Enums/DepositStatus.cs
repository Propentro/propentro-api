namespace Propentro.Domain.Enums;

/// <summary>
/// Status of the security deposit.
/// </summary>
public enum DepositStatus
{
    Pending,
    Held,
    PartiallyReturned,
    Returned,
    Forfeited
}
