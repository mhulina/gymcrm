using System.ComponentModel.DataAnnotations;

namespace GymCRM.MembershipAPI.Infrastructure.Entities
{
    public class Member : BaseEntity
    {
		[Required]
		public string HashedPassword { get; set; }
		[Required]
        public int UserType { get; set; }
        [Required]
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        [Required]
        public string LastName { get; set; }
        [Required]
        public int Gender { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
        [Required]
        public DateTime DateJoined { get; set; }
        public Guid? PersonalTrainerId { get; set; }
        public List<Guid>? WorkoutGroupIds { get; set; }
        public int? WorkingExperienceInMonths {  get; set; }
        public int GymSubscriptionType { get; set; }
    }
}
