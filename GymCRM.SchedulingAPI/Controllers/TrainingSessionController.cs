using Asp.Versioning;
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

    public TrainingSessionController(ITrainingSessionsService trainingSessionService)
    {
        _trainingSessionService = trainingSessionService ?? throw new ArgumentNullException(nameof(trainingSessionService));
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
}