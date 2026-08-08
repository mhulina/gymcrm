using System.Security.Claims;
using Asp.Versioning;
using GymCRM.SchedulingAPI.Models;
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
    private readonly ITrainerAvailabilitiesService _trainerAvailabilitiesService;

    public AvailabilitiesController(ITrainerAvailabilitiesService trainerAvailabilitiesService)
    {
        _trainerAvailabilitiesService = trainerAvailabilitiesService ?? throw new ArgumentNullException(nameof(trainerAvailabilitiesService));
    }

    /// <summary>
    /// Retrieves all availability records.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> containing an enumerable collection of <see cref="TrainerAvailability"/> objects.
    /// </returns>
    /// <response code="200">Returns the list of availabilities.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrainerAvailability>>> GetAvailabilities(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainerAvailabilitiesService.GetAvailabilitiesAsync(cancellationToken: cancellationToken);

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
    /// An <see cref="ActionResult{T}"/> containing an enumerable collection of <see cref="TrainerAvailability"/> objects for the given trainer.
    /// </returns>
    /// <response code="200">Returns the list of availabilities for the specified trainer.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IEnumerable<TrainerAvailability>>> GetAvailabilitiesForTrainerId(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainerAvailabilitiesService.GetAvailabilitiesForTrainerIdAsync(
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
    /// Retrieves the TrainerIds of every trainer with at least one bookable working-hours
    /// range, for filtering trainers with no configured hours out of the booking flow.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="ActionResult{T}"/> containing an enumerable collection of trainer GUIDs.
    /// </returns>
    /// <response code="200">Returns the list of trainer GUIDs with working hours configured.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Guid>>> GetTrainerIdsWithWorkingHours(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainerAvailabilitiesService.GetTrainerIdsWithWorkingHoursAsync(cancellationToken: cancellationToken);

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
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPost]
    public async Task<ActionResult> AddAvailability([FromBody] InsertAvailability insertAvailability, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.AddAvailabilityAsync(
                insertAvailability, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new CreatedResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPost("{trainerId:guid}/{nameOfDay}/workinghours")]
    public async Task<ActionResult<bool>> AddWorkingHoursToDailyAvailability(
        [FromRoute] Guid trainerId,
        [FromRoute] string nameOfDay,
        [FromBody] List<InsertWorkingHours> insertWorkingHours,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.AddWorkingHoursToDailyAvailability(
                trainerId,
                nameOfDay,
                insertWorkingHours,
                callerAccountGuid,
                callerIsAdmin,
                cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new CreatedResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
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
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteAvailability(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.DeleteAvailabilityAsync(
                id, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates an existing availability record.
    /// </summary>
    /// <param name="updatedTrainerAvailability">The updated <see cref="TrainerAvailability"/> object.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result indicating the outcome of the update operation.
    /// </returns>
    /// <response code="204">The availability was successfully updated.</response>
    /// <response code="400">The request data was invalid or the update failed.</response>
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut]
    public async Task<ActionResult> UpdateAvailability(
        [FromBody] TrainerAvailability updatedTrainerAvailability,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.UpdateAvailabilityAsync(
                updatedTrainerAvailability, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Updates the start/end time of an existing working-hours range.
    /// </summary>
    /// <param name="id">The unique identifier of the working-hours range to update.</param>
    /// <param name="updatedWorkingHours">The new start/end time.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="204">The working hours were successfully updated.</response>
    /// <response code="400">The request data was invalid, or the working hours could not be found.</response>
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateWorkingHours(
        Guid id,
        [FromBody] InsertWorkingHours updatedWorkingHours,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.UpdateWorkingHoursAsync(
                id, updatedWorkingHours, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Deletes an existing working-hours range.
    /// </summary>
    /// <param name="id">The unique identifier of the working-hours range to delete.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="204">The working hours were successfully deleted.</response>
    /// <response code="400">The working hours could not be found or deletion failed.</response>
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteWorkingHours(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.DeleteWorkingHoursAsync(
                id, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Sets whether a given day of the week is a day off for a trainer.
    /// </summary>
    /// <param name="trainerId">The unique identifier of the trainer.</param>
    /// <param name="nameOfDay">The name of the day of the week (e.g., "Monday").</param>
    /// <param name="request">The desired day-off status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <response code="204">The day-off status was successfully applied.</response>
    /// <response code="400">The request data was invalid or the update failed.</response>
    /// <response code="403">The caller is not allowed to modify this trainer's availability.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut("{trainerId:guid}/{nameOfDay}")]
    public async Task<ActionResult> SetDayOffStatus(
        Guid trainerId,
        string nameOfDay,
        [FromBody] UpdateDayOffStatus request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainerAvailabilitiesService.SetDayOffStatusAsync(
                trainerId, nameOfDay, request.IsDayOff, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainerAvailabilityAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private bool TryGetCallerIdentity(out Guid callerAccountGuid, out bool callerIsAdmin)
    {
        callerAccountGuid = Guid.Empty;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        callerIsAdmin = string.Equals(User.FindFirst("type")?.Value, "Admin", StringComparison.Ordinal);

        return !string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out callerAccountGuid);
    }
}
