namespace GymCRM.BillingAPI.Models.DTOs;

public class Payment
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public int Method { get; set; }
    public int Status { get; set; }
    public DateTime PaidAt { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime DateCreated { get; set; }
}
