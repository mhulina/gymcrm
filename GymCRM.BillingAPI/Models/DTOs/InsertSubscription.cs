using System.ComponentModel.DataAnnotations;

namespace GymCRM.BillingAPI.Models.DTOs;

public class InsertSubscription
{
    [Required]
    public Guid MemberAccountGuid { get; set; }
    [Required]
    public int PlanType { get; set; }
}
