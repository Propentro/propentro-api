namespace Propentro.Domain.Enums;

/// <summary>
/// Status of a compliance item (certificate, inspection, etc).
/// </summary>
public enum ComplianceStatus
{
    Valid,
    ExpiringSoon,
    Expired,
    Pending,
    NotRequired
}
