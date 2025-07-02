using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCRM.MembershipAPI.Infrastructure.Entities
{
    public class Member
    {
	    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	    public int Id { get; set; }
		[Required]
        public int AccountType { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        [Required]
        public int Gender { get; set; }
        [Required]
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
        public DateTime DateModified { get; set; }
        public Guid? PersonalTrainerId { get; set; }
        public List<Guid>? WorkoutGroupIds { get; set; }
        public int? WorkingExperienceInMonths {  get; set; }
        public int GymSubscriptionType { get; set; }
        [Required]
        public Guid AccountGuid { get; set; }
        public Account Account { get; set; }
    }
}
