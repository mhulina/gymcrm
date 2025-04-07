using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface;

public interface IAuthenticationService
{
	Guid RegisterAccount(AccountDto accountDto);
	string LoginAccount(AuthenticationRequestBody accountDto);
	bool DeleteAccount(Guid accountGuid);
}