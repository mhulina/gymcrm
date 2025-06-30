using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface
{
	public interface IMembersService
	{
		List<Member> GetAllUsers();
		Member GetByGuid(Guid guid);
		Member GetByEmail(string email);
		bool UpdateMember(Member insertMember);
		bool InsertMember(InsertMember insertMember);
	}
}
