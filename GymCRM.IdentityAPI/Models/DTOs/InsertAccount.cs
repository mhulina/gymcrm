using System.ComponentModel.DataAnnotations;

namespace GymCRM.IdentityAPI.Models.DTOs;

public class InsertAccount
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public int? AccountType { get; set; }
    public int? Gender { get; set; }
    public string? TimeZone { get; set; }
}