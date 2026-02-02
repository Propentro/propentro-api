using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Lease : AggregateRoot
{
    public string LeaseType { get; set; } = "fixed_term";
    public string Status { get; set; } = "draft";
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly? MoveInDate { get; set; }
    public DateOnly? MoveOutDate { get; set; }
    public string? ReferenceCode { get; set; }

    // Rent (minor units)
    public long? RentMinor { get; set; }
    public string Currency { get; set; } = "EUR";
    public string RentFrequency { get; set; } = "monthly";
    public int? RentDueDay { get; set; }

    // Deposit (minor units)
    public long? DepositMinor { get; set; }
    public string? DepositStatus { get; set; }
    public string? DepositHeldBy { get; set; }
    public DateOnly? DepositReceivedAt { get; set; }
    public DateOnly? DepositReturnedAt { get; set; }
    public long? DepositReturnMinor { get; set; }

    // Renewal tracking
    public Guid? PreviousLeaseId { get; set; }

    // Notice
    public int? NoticePeriodDays { get; set; }

    public string? Note { get; set; }

    // Navigation
    public Lease? PreviousLease { get; set; }
    public ICollection<LeaseUnit> LeaseUnits { get; set; } = [];
    public ICollection<LeaseParty> LeaseParties { get; set; } = [];
    public ICollection<LeaseDocument> LeaseDocuments { get; set; } = [];
    public ICollection<LeaseCompliance> LeaseCompliances { get; set; } = [];
}
