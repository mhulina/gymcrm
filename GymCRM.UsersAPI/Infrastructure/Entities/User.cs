using System.ComponentModel.DataAnnotations;

namespace GymCRM.UsersAPI.Infrastructure.Entities
{
    public class User : BaseEntity
    {
        [Required]
        public int UserType { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public string? MobilePhone { get; set; }
        [Required]
        public DateTime DateJoined { get; set; }
        public int? PersonalTrainerId { get; set; }
        public int? WorkoutGroupId { get; set; }
    }
}
