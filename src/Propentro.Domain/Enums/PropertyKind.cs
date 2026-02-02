namespace Propentro.Domain.Enums;

/// <summary>
/// Type of property structure.
/// Use string values in entities to match DB CHECK constraints.
/// The enum is for type-safety in business logic.
/// </summary>
public enum PropertyKind
{
    Standalone,
    Building,
    Complex,
    Land
}
