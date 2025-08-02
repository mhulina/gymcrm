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
// [Authorize]
[ApiController]
public class TrainingSessionController : ControllerBase
{
    private readonly ITrainingSessionsService _trainingSessionService;

    public TrainingSessionController(ITrainingSessionsService trainingSessionService)
    {
        _trainingSessionService = trainingSessionService ?? throw new ArgumentNullException(nameof(trainingSessionService));
    }

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
            return new StatusCodeResult(500);
        }
    }

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
            return new StatusCodeResult(500);
        }
    }
}