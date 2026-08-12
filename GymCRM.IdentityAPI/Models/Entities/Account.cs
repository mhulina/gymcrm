using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCRM.IdentityAPI.Models.Entities;

public class Account
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public string Email { get; set; }
    [Required]
    public DateTime DateCreated { get; set; }
    [Required]
    public string HashPepper { get; set; }
    [Required]
    public string HashedPassword { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
    // Set when this account's password was assigned by someone else (an Admin creating the
    // account on the user's behalf) rather than chosen by the user themselves - cleared the
    // moment the user successfully changes it via ChangePassword.
    public bool MustChangePassword { get; set; }

    public virtual Member Member { get; set; }
}