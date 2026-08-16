using System.Security.Claims;
using Asp.Versioning;
using GymCRM.BillingAPI.Models.DTOs;
using GymCRM.BillingAPI.Models.Exceptions;
using GymCRM.BillingAPI.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCRM.BillingAPI.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]/[action]")]
[Authorize]
[ApiController]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionsService _subscriptionsService;

    public SubscriptionsController(ISubscriptionsService subscriptionsService)
    {
        _subscriptionsService = subscriptionsService;
    }

    [HttpPost]
    public async Task<ActionResult<Subscription>> CreateSubscription(
        [FromBody] InsertSubscription insertSubscription,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out _, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.CreateSubscriptionAsync(insertSubscription, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Subscription>> GetSubscriptionById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.GetSubscriptionByIdAsync(id, callerAccountGuid, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{memberAccountGuid:guid}")]
    public async Task<ActionResult<Subscription?>> GetActiveSubscriptionForMember(
        Guid memberAccountGuid,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.GetActiveSubscriptionForMemberAsync(
                memberAccountGuid,
                callerAccountGuid,
                callerIsAdmin,
                cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("{memberAccountGuid:guid}")]
    public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptionsForMember(
        Guid memberAccountGuid,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.GetSubscriptionsForMemberAsync(
                memberAccountGuid,
                callerAccountGuid,
                callerIsAdmin,
                cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Subscription>> RenewSubscription(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out _, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.RenewSubscriptionAsync(id, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (SubscriptionNotRenewableException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Subscription>> CancelSubscription(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.CancelSubscriptionAsync(id, callerAccountGuid, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (SubscriptionAccessDeniedException ex)
        {
            return new ObjectResult(ex.Message) { StatusCode = StatusCodes.Status403Forbidden };
        }
        catch (Exception)
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Subscription>> MarkSubscriptionPastDue(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out _, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _subscriptionsService.MarkSubscriptionPastDueAsync(id, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (SubscriptionNotFoundException)
        {
            return new NotFoundResult();
        }
        catch (SubscriptionAccessDeniedException ex)
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
