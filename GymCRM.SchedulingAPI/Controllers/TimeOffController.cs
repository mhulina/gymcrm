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
public class TimeOffController : ControllerBase
{
    private readonly ITimeOffService _timeOffService;

    public TimeOffController(ITimeOffService timeOffService)
    {
        _timeOffService = timeOffService;
    }

    /// <summary>
    /// Retrieves all time-off records.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="200">Returns a list of all time-off records.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TimeOff>>> GetAllTimeOffs(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.GetAllAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves all time-off records for the specified trainer.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="200">Returns a list of time-off records for the specified trainer.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{trainerId:guid}")]
    public async Task<ActionResult<IEnumerable<TimeOff>>> GetAllForTrainerId(
        Guid trainerId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.GetAllForTrainerIdAsync(
                trainerId,
                cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves all time-off records for a given date range.
    /// </summary>
    /// <param name="startDate">The start date of the period to search.</param>
    /// <param name="endDate">The end date of the period to search.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="200">Returns a dictionary mapping dates to time-off records within the specified period.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<IDictionary<DateTime, TimeOff>>> GetAllForDatePeriod(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.GetAllForDatePeriodAsync(
                startDate, 
                endDate, 
                cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Adds a new time-off record.
    /// </summary>
    /// <param name="insertTimeOff">The DTO containing time-off details to add.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="201">If the time-off was successfully created.</response>
    /// <response code="400">If the request data is invalid.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    public async Task<ActionResult> AddNewTimeOff(InsertTimeOff insertTimeOff, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.AddTimeOffAsync(insertTimeOff, cancellationToken: cancellationToken);
            
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

    /// <summary>
    /// Deletes a time-off record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the time-off record to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="200">If the time-off was successfully deleted.</response>
    /// <response code="400">If the deletion could not be performed.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteTimeOff(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.DeleteTimeOffAsync(id, cancellationToken: cancellationToken);
            
            if (!result)
            {
                return new BadRequestResult();
            }
            
            return new OkResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing time-off record.
    /// </summary>
    /// <param name="updatedTimeOff">The updated time-off object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="200">If the time-off was successfully updated.</response>
    /// <response code="400">If the update could not be performed.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut]
    public async Task<ActionResult> UpdateTimeOff(
        [FromBody] TimeOff updatedTimeOff, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _timeOffService.UpdateTimeOffAsync(updatedTimeOff, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }
            
            return new OkResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}