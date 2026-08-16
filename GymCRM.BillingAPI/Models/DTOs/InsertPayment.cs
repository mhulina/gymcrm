using System.ComponentModel.DataAnnotations;

namespace GymCRM.BillingAPI.Models.DTOs;

public class InsertPayment
{
    [Required]
    public Guid SubscriptionId { get; set; }
    [Required]
    public decimal Amount { get; set; }
    [Required]
    public int Method { get; set; }
    [Required]
    public int Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? ExternalReference { get; set; }
}
