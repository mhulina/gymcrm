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
public class PaymentsController : ControllerBase
{
    private readonly IPaymentsService _paymentsService;

    public PaymentsController(IPaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> RecordPayment(
        [FromBody] InsertPayment insertPayment,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out _, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _paymentsService.RecordPaymentAsync(insertPayment, callerIsAdmin, cancellationToken);

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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Payment>> GetPaymentById(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _paymentsService.GetPaymentByIdAsync(id, callerAccountGuid, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (PaymentNotFoundException)
        {
            return new NotFoundResult();
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

    [HttpGet("{subscriptionId:guid}")]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsForSubscription(
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _paymentsService.GetPaymentsForSubscriptionAsync(
                subscriptionId,
                callerAccountGuid,
                callerIsAdmin,
                cancellationToken);

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
    public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsForMember(
        Guid memberAccountGuid,
        CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out var callerAccountGuid, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _paymentsService.GetPaymentsForMemberAsync(
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
    public async Task<ActionResult<Payment>> RefundPayment(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCallerIdentity(out _, out var callerIsAdmin))
        {
            return new UnauthorizedObjectResult("Invalid token claims");
        }

        try
        {
            var result = await _paymentsService.RefundPaymentAsync(id, callerIsAdmin, cancellationToken);

            return new OkObjectResult(result);
        }
        catch (PaymentNotFoundException)
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
