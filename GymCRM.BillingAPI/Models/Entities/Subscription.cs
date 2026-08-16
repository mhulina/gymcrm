using System.ComponentModel.DataAnnotations;

namespace GymCRM.BillingAPI.Models.Entities;

public class Subscription
{
    [Required]
    public Guid Id { get; set; }
    // The member's Account.Id in GymCRM.IdentityAPI - a bare GUID, not a foreign key, since
    // Billing has its own database and never references Identity's schema directly (same
    // cross-module convention Scheduling already uses for TrainerId/ClientId).
    [Required]
    public Guid MemberAccountGuid { get; set; }
    [Required]
    public int PlanType { get; set; }
    [Required]
    public int Status { get; set; }
    [Required]
    public DateTime StartDate { get; set; }
    // Null once Status is Cancelled/Expired - there's nothing left to renew.
    public DateTime? NextRenewalDate { get; set; }
    [Required]
    public DateTime DateCreated { get; set; }
    [Required]
    public DateTime DateModified { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
