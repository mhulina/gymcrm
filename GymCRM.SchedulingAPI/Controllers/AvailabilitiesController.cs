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

    /// <summary>
    /// Retrieves all availability records.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> containing an enumerable collection of <see cref="Availability"/> objects.
    /// </returns>
    /// <response code="200">Returns the list of availabilities.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
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

    /// <summary>
    /// Retrieves all availability records for a specific trainer.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> containing an enumerable collection of <see cref="Availability"/> objects for the given trainer.
    /// </returns>
    /// <response code="200">Returns the list of availabilities for the specified trainer.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IEnumerable<Availability>>> GetAvailabilitiesForTrainerId(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _availabilitiesService.GetAvailabilitiesForTrainerIdAsync(
                id, 
                cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    /// <summary>
    /// Adds a new availability record.
    /// </summary>
    /// <param name="insertAvailability">The DTO containing the new availability details.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result indicating the outcome of the creation operation.
    /// </returns>
    /// <response code="201">The availability was successfully created.</response>
    /// <response code="400">The request data was invalid or the availability could not be created.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
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

    /// <summary>
    /// Deletes an existing availability record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the availability to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result indicating the outcome of the deletion operation.
    /// </returns>
    /// <response code="204">The availability was successfully deleted.</response>
    /// <response code="400">The specified availability could not be found or deletion failed.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAvailability(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _availabilitiesService.DeleteAvailabilityAsync(id, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }
            
            return new NoContentResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing availability record.
    /// </summary>
    /// <param name="updatedAvailability">The updated <see cref="Availability"/> object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result indicating the outcome of the update operation.
    /// </returns>
    /// <response code="204">The availability was successfully updated.</response>
    /// <response code="400">The request data was invalid or the update failed.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut]
    public async Task<ActionResult> UpdateAvailability(
        [FromBody] Availability updatedAvailability,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _availabilitiesService.UpdateAvailabilityAsync(
                updatedAvailability, 
                cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }
            
            return new NoContentResult();
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}