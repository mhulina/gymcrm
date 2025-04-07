using System.ComponentModel.DataAnnotations;

namespace GymCRM.MembershipAPI.Models.DTOs;

public class AccountDto
{
    public Guid? Guid { get; set; }

    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public int? AccountType { get; set; }
    public int? GymSubscriptionType { get; set; }
    public int? Gender { get; set; }
}