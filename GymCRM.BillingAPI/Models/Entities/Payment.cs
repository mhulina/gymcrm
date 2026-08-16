using System.ComponentModel.DataAnnotations;

namespace GymCRM.BillingAPI.Models.Entities;

public class Payment
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public Guid SubscriptionId { get; set; }
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public int Method { get; set; }
    [Required]
    public int Status { get; set; }
    [Required]
    public DateTime PaidAt { get; set; }
    // Payment-gateway transaction/charge ID for card payments; null for manually-recorded
    // cash/bank-transfer payments taken at the front desk.
    public string? ExternalReference { get; set; }
    [Required]
    public DateTime DateCreated { get; set; }

    public virtual Subscription Subscription { get; set; } = null!;
}
