using System.ComponentModel.DataAnnotations;

namespace GymCRM.MembershipAPI.Models.DTOs
{
	public class UserDto
	{
		public Guid Guid { get; set; }
		public string UserPassword { get; set; }
		public int UserType { get; set; }
		public string FirstName { get; set; }
		public string MiddleName { get; set; }
		public string LastName { get; set; }
		public string Email { get; set; }
		public string PhoneNumber { get; set; }
		public string MobilePhone { get; set; }
		public DateTime DateJoined { get; set; }
		public int PersonalTrainerId { get; set; }
		public int WorkoutGroupId { get; set; }
	}
}
