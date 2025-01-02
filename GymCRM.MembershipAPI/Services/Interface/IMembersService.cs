using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface
{
	public interface IMembersService
	{
		List<MemberDto> GetAllUsers();
		MemberDto GetByGuid(Guid guid);
		MemberDto GetByEmail(string email);
		bool UpdateMember(MemberDto newMemberDto);
		bool InsertMember(MemberDto newMemberDto);
	}
}
