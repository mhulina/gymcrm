using GymCRM.IdentityAPI.Models.Enums;

namespace GymCRM.IdentityAPI.Models.DTOs
{
	public class Member
	{
		public Guid? AccountGuid { get; set; }
		public AccountType AccountType { get; set; }
		public string? FirstName { get; set; }
		public string? MiddleName { get; set; }
		public string? LastName { get; set; }
		public string Email { get; set; }
		public string? PhoneNumber { get; set; }
		public string? MobileNumber { get; set; }
		public int Gender { get; set; }
		public Guid? PersonalTrainerId { get; set; }
		public List<Guid>? WorkoutGroupIds { get; set; }
        public int? WorkingExperienceInMonths {  get; set; }
        public int GymSubscriptionType { get; set; }
	}
}
