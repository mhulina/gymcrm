using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Implementation;

namespace GymCRM.MembershipAPI.Services.Interface;

public interface IAuthenticationService
{
	Guid RegisterAccount(AccountDto accountDto);
	string LoginAccount(AuthenticationRequestBody accountDto);
	bool DeleteAccount(Guid accountGuid);
}