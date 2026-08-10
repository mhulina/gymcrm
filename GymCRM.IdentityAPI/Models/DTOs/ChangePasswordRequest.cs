using System.ComponentModel.DataAnnotations;

namespace GymCRM.IdentityAPI.Models.DTOs;

// Body for POST /Authentication/ChangePassword. Deliberately has no Email field - the caller's
// identity comes from their JWT claims, not a client-supplied value, so an authenticated user
// can never change another account's password by editing the request body.
public class ChangePasswordRequest
{
    [Required]
    public string OldPassword { get; set; }
    [Required]
    public string NewPassword { get; set; }
}
