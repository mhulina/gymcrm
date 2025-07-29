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
    public string HashSalt { get; set; }
    [Required]
    public string HashedPassword { get; set; }
    
    public Member Member { get; set; }
}