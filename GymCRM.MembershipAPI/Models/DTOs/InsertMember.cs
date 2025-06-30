using GymCRM.MembershipAPI.Models.Enums;

namespace GymCRM.MembershipAPI.Models.DTOs;

public class InsertMember
{
    public AccountType AccountType { get; set; }
    public string Email { get; set; }
    public int? WorkingExperienceInMonths {  get; set; }
    public int GymSubscriptionType { get; set; }
}