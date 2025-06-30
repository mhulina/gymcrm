using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface;

public interface IAuthenticationService
{
	Guid RegisterAccount(InsertAccount account);
	string LoginAccount(AuthenticationRequestBody accountDto);
	bool DeleteAccount(Guid accountGuid);
}