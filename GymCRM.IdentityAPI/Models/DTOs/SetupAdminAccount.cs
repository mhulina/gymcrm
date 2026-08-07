using System.ComponentModel.DataAnnotations;

namespace GymCRM.IdentityAPI.Models.DTOs;

// Body for POST /Authentication/SetupAdminAccount - a dedicated, slim DTO rather than
// reusing InsertAccount, which carries an AccountType field that would be misleading here
// (this endpoint always creates an Admin, regardless of what a caller might pass).
public class SetupAdminAccount
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
    public string? TimeZone { get; set; }
}
