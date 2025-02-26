using System.IdentityModel.Tokens.Jwt;
using System.Security.Authentication;
using System.Security.Claims;
using Asp.Versioning;
using GymCRM.MembershipAPI.Models.DTOs;
using GymCRM.MembershipAPI.Services.Interface;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GymCRM.MembershipAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[ApiController]
public class AccountsController : ControllerBase
{
    private readonly IAccountsService _accountsService;
    private readonly IConfiguration _configuration;
    private ResponseDto _responseDto;

    public AccountsController(IAccountsService accountsService, IConfiguration configuration)
    {
        _accountsService = accountsService ?? throw new ArgumentNullException(nameof(accountsService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _responseDto = new ResponseDto();
    }
    
    [HttpPost]
    public ActionResult<Guid> Register([FromBody]AccountDto accountDto)
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
    public ActionResult Login([FromBody]AuthenticationRequestBody authenticationRequest)
    {
        try
        {
            var authenticationResult = _accountsService.LoginAccount(authenticationRequest);

            if (!authenticationResult.Success)
            {
                return new UnauthorizedResult();
            }

            var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(_configuration["Authentication:SecretForKey"]));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimsForToken = new List<Claim>();
            claimsForToken.Add(new Claim("sub", authenticationResult.AccountDto.Guid.ToString()));
            claimsForToken.Add(new Claim("email", authenticationResult.AccountDto.Email));

            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsForToken,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(30),
                signingCredentials);
            
            var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
            
            return new JsonResult(tokenToReturn);
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
}