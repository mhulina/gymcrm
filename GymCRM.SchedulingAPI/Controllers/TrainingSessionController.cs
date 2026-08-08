using System.Security.Claims;
using Asp.Versioning;
using GymCRM.SchedulingAPI.Models;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using TrainingSession = GymCRM.SchedulingAPI.Models.DTOs.TrainingSession;

namespace GymCRM.SchedulingAPI.Controllers;

[EnableCors("AllowAny")]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[action]")]
[Authorize]
[ApiController]
public class TrainingSessionController : ControllerBase
{
    private readonly ITrainingSessionsService _trainingSessionService;
    private readonly IBookingValidationService _bookingValidationService;

    public TrainingSessionController(
        ITrainingSessionsService trainingSessionService,
        IBookingValidationService bookingValidationService)
    {
        _trainingSessionService = trainingSessionService ?? throw new ArgumentNullException(nameof(trainingSessionService));
        _bookingValidationService = bookingValidationService ?? throw new ArgumentNullException(nameof(bookingValidationService));
    }

    /// <summary>
    /// Retrieves a list of all training sessions.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="ActionResult"/> containing a list of <see cref="TrainingSession"/> objects if successful.
    /// </returns>
    /// <response code="200">Returns the list of training sessions.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSession>>> GetAllTrainingSessions(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetAllAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Retrieves a list of all cancelled training sessions.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of cancelled <see cref="TrainingSession"/> objects.</returns>
    /// <response code="200">Returns the list of cancelled training sessions.</response>
    /// <response code="500">Returned if an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSession>>> GetAllCancelledTrainingSessions(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetCancelledTrainingSessionsAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    /// <summary>
    /// Retrieves a list of all completed training sessions.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of completed <see cref="TrainingSession"/> objects.</returns>
    /// <response code="200">Returns the list of completed training sessions.</response>
    /// <response code="500">Returned if an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSession>>> GetAllCompletedTrainingSessions(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetCompletedTrainingSessionsAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
    
    /// <summary>
    /// Retrieves a list of all pending training sessions.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A list of pending <see cref="TrainingSession"/> objects.</returns>
    /// <response code="200">Returns the list of pending training sessions.</response>
    /// <response code="500">Returned if an internal server error occurs.</response>
    [HttpGet]
    public async Task<ActionResult<List<TrainingSession>>> GetAllPendingTrainingSessions(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetPendingTrainingSessionsAsync(cancellationToken: cancellationToken);
            
            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Adds a new training session.
    /// </summary>
    /// <param name="trainingSession">The training session data to insert.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// An <see cref="ActionResult"/> indicating the result of the operation.
    /// </returns>
    /// <response code="201">If the training session was successfully created.</response>
    /// <response code="400">If the input is invalid or the session could not be created.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    public async Task<ActionResult> AddTrainingSession(
        InsertTrainingSession trainingSession,
        CancellationToken cancellationToken)
    {
        try
        {
            var trainingSessionValidation = await _bookingValidationService.ValidateBookingAsync(trainingSession, cancellationToken);

            if (!trainingSessionValidation.IsValid)
            {
                return new BadRequestObjectResult(string.Join("\n ", trainingSessionValidation.Errors));
            }
            
            var result = await _trainingSessionService.InsertTrainingSessionAsync(trainingSession, cancellationToken);

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
    /// Deletes a training session by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the training session to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// Returns <see cref="OkResult"/> if the deletion was successful,
    /// <see cref="BadRequestResult"/> if the deletion failed,
    /// or <see cref="StatusCodeResult"/> with status code 500 if an exception occurs.
    /// </returns>
    /// <response code="200">If the training session was deleted successfully.</response>
    /// <response code="400">If the input is invalid or the session could not be deleted.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteTrainingSession(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.DeleteTrainingSessionAsync(id, cancellationToken: cancellationToken);

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
    /// Updates an existing training session with new data.
    /// </summary>
    /// <param name="updatedTrainingSession">The updated <see cref="TrainingSession"/> object to be saved.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>An appropriate HTTP status code indicating the result of the operation.</returns>
    /// <response code="200">Returned if the training session was successfully updated.</response>
    /// <response code="400">Returned if the update request was invalid or failed validation.</response>
    /// <response code="500">Returned if an internal server error occurs during the update.</response>
    [HttpPut]
    public async Task<ActionResult> UpdateTrainingSession(
        [FromBody] TrainingSession updatedTrainingSession,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.UpdateTrainingSessionAsync(
                updatedTrainingSession,
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

    /// <summary>
    /// Retrieves all training sessions associated with a specific client.
    /// </summary>
    /// <param name="id">The unique identifier of the client.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A list of training sessions for the specified client.
    /// </returns>
    /// <response code="200">Returns the list of training sessions for the specified client.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IEnumerable<TrainingSession>>> GetAllTrainingSessionsForClient(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetTrainingSessionsForClientIdAsync(
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
    /// Retrieves all training sessions associated with a specific trainer.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    /// A list of training sessions for the specified trainer.
    /// </returns>
    /// <response code="200">Returns the list of training sessions for the specified trainer.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IEnumerable<TrainingSession>>> GetTrainingSessionsForTrainerId(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _trainingSessionService.GetTrainingSessionsForTrainerIdAsync(
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
    /// Computes the bookable start times for a trainer on a given date, and the durations that
    /// fit at each one.
    /// </summary>
    /// <param name="id">The unique identifier of the trainer.</param>
    /// <param name="date">The date to compute slots for, in the trainer's own local time.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <response code="200">Returns the available slots for that date (empty if none).</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<List<AvailableSlot>>> GetAvailableSlotsForTrainer(
        Guid id,
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _bookingValidationService.GetAvailableSlotsAsync(id, date, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Accepts a requested training session, promoting it to Booked.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <response code="204">The session was successfully accepted.</response>
    /// <response code="400">The session could not be found or was not in a requested state.</response>
    /// <response code="403">The caller is not allowed to modify this training session.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> AcceptTrainingSession(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainingSessionService.AcceptTrainingSessionAsync(
                id, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainingSessionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Declines a requested training session, setting it to Cancelled.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <response code="204">The session was successfully declined.</response>
    /// <response code="400">The session could not be found or was not in a requested state.</response>
    /// <response code="403">The caller is not allowed to modify this training session.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> DeclineTrainingSession(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _trainingSessionService.DeclineTrainingSessionAsync(
                id, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainingSessionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Reschedules a requested training session to a new time, re-running the same booking
    /// validation used when the session was first requested, then promotes it to Booked.
    /// </summary>
    /// <param name="id">The unique identifier of the training session.</param>
    /// <param name="request">The new start/end time.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <response code="204">The session was successfully rescheduled.</response>
    /// <response code="400">The session could not be found, was not in a requested state, or the new time failed validation.</response>
    /// <response code="403">The caller is not allowed to modify this training session.</response>
    /// <response code="500">An unexpected error occurred on the server.</response>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> RescheduleTrainingSession(
        Guid id,
        [FromBody] RescheduleTrainingSession request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var existingSession = await _trainingSessionService.GetTrainingSessionByIdAsync(id, cancellationToken);

            if (existingSession is null)
            {
                return new BadRequestResult();
            }

            if (existingSession.TrainerId != callerAccountGuid && !callerIsAdmin)
            {
                return new ObjectResult("You are not allowed to modify this training session")
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var candidateBooking = new InsertTrainingSession
            {
                TrainerId = existingSession.TrainerId,
                ClientId = existingSession.ClientId,
                StartTime = request.NewStartTime,
                EndTime = request.NewEndTime,
                Description = existingSession.Description
            };

            var validation = await _bookingValidationService.ValidateBookingAsync(
                candidateBooking, cancellationToken, excludeSessionId: id);

            if (!validation.IsValid)
            {
                return new BadRequestObjectResult(string.Join("\n ", validation.Errors));
            }

            var result = await _trainingSessionService.RescheduleTrainingSessionAsync(
                id, request.NewStartTime, request.NewEndTime, callerAccountGuid, callerIsAdmin, cancellationToken: cancellationToken);

            if (!result)
            {
                return new BadRequestResult();
            }

            return new NoContentResult();
        }
        catch (TrainingSessionAccessDeniedException ex)
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