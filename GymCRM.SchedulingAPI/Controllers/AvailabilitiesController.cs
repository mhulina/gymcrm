using Asp.Versioning;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.SchedulingAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[Authorize]
[ApiController]
public class AvailabilitiesController : ControllerBase
{
    private readonly IAvailabilitiesService _availabilitiesService;

    public AvailabilitiesController(IAvailabilitiesService availabilitiesService)
    {
        _availabilitiesService = availabilitiesService ?? throw new ArgumentNullException(nameof(availabilitiesService));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Availability>>> GetAvailabilities(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _availabilitiesService.GetAvailabilitiesAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost]
    public async Task<ActionResult> AddAvailability([FromBody] InsertAvailability insertAvailability, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _availabilitiesService.AddAvailabilityAsync(insertAvailability, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new CreatedResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}