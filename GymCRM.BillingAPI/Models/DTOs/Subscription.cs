namespace GymCRM.BillingAPI.Models.DTOs;

public class Subscription
{
    public Guid Id { get; set; }
    public Guid MemberAccountGuid { get; set; }
    public int PlanType { get; set; }
    public int Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? NextRenewalDate { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}
