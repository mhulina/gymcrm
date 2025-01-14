using GymCRM.MembershipAPI.Models.DTOs;

namespace GymCRM.MembershipAPI.Services.Interface;

public interface IAccountsService
{
    Guid RegisterAccount(AccountDto accountDto);
    bool LoginAccount(AuthenticationRequestBody accountDto);
    bool DeleteAccount(Guid accountGuid);
}