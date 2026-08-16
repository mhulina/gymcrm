using GymCRM.IdentityAPI.Models.Enums;

namespace GymCRM.IdentityAPI.Models.DTOs;

public class InsertMember
{
    public AccountType AccountType { get; set; }
    public string Email { get; set; }
    public int? WorkingExperienceInMonths {  get; set; }
}