using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCRM.MembershipAPI.Infrastructure.Entities;

public class Account
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    public Guid Guid { get; set; }
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