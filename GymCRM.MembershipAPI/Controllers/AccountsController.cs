using System.Security.Authentication;
using Asp.Versioning;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.MembershipAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[ApiController]
public class AccountsController
{
    private readonly IAccountsService _accountsService;
    private ResponseDto _responseDto;

    public AccountsController(IAccountsService accountsService)
    {
        _accountsService = accountsService;
        _responseDto = new ResponseDto();
    }
    
    [HttpPost]
    public ActionResult<Guid> RegisterAccount([FromBody]AccountDto accountDto)
    {
        try
        {
            var registeredAccountGuid = _accountsService.RegisterAccount(accountDto);

            if (registeredAccountGuid == Guid.Empty)
            {
                return new StatusCodeResult(500);
            }
            
            return new CreatedResult();
        }
        catch (Exception)
        {
            return new BadRequestResult();
        }
    }

    [HttpPost]
    public ActionResult LoginAccount([FromBody]AuthenticationRequestBody accountDto)
    {
        try
        {
            var loginSuccess = _accountsService.LoginAccount(accountDto);

            if (loginSuccess)
            {
                return new OkResult();
            }

            return new UnauthorizedResult();
        }
        catch (AuthenticationException)
        {
            return new ForbidResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(500);
        }
    }

    [HttpDelete("{guid}")]
    public ActionResult DeleteAccount(Guid guid)
    {
        try
        {
            var result = _accountsService.DeleteAccount(guid);

            if (result)
            {
                return new OkResult();
            }
            
            return new NotFoundResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(500);
        }
    }
}