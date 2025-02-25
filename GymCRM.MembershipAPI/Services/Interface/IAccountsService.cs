using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Implementation;

namespace GymCRM.MembershipAPI.Services.Interface;

public interface IAccountsService
{
    Guid RegisterAccount(AccountDto accountDto);
    AccountsService.AuthenticationResult LoginAccount(AuthenticationRequestBody accountDto);
    bool DeleteAccount(Guid accountGuid);
}