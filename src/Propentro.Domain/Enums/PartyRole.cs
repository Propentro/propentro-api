namespace Propentro.Domain.Enums;

/// <summary>
/// Role a party plays in relation to a property, unit, or lease.
/// </summary>
public enum PartyRoleType
{
    Owner,
    Landlord,
    Tenant,
    Guarantor,
    Agent,
    PropertyManager,
    Contractor,
    EmergencyContact
}
