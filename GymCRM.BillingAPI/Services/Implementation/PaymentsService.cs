using AutoMapper;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.DTOs;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Exceptions;
using GymCRM.BillingAPI.Models.Interface;
using GymCRM.BillingAPI.Services.Interface;

namespace GymCRM.BillingAPI.Services.Implementation;

public class PaymentsService : IPaymentsService
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ISubscriptionsService _subscriptionsService;

    public PaymentsService(
        IPaymentsRepository paymentsRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ISubscriptionsService subscriptionsService)
    {
        _paymentsRepository = paymentsRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _subscriptionsService = subscriptionsService;
    }

    public async Task<Payment> RecordPaymentAsync(
        InsertPayment insertPayment,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        if (insertPayment is null)
        {
            throw new ArgumentNullException(nameof(insertPayment));
        }

        EnsureAdmin(callerIsAdmin);

        var subscription = await _subscriptionsService.GetSubscriptionByIdAsync(
            insertPayment.SubscriptionId,
            Guid.Empty,
            callerIsAdmin: true,
            cancellationToken);

        var now = DateTime.UtcNow;
        var payment = new Models.Entities.Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = insertPayment.SubscriptionId,
            Amount = insertPayment.Amount,
            Method = insertPayment.Method,
            Status = insertPayment.Status,
            PaidAt = insertPayment.PaidAt ?? now,
            ExternalReference = insertPayment.ExternalReference,
            DateCreated = now
        };

        _paymentsRepository.Insert(payment);
        await _unitOfWork.SaveAsync(cancellationToken);

        if (insertPayment.Status == (int)PaymentStatus.Succeeded && subscription.Status == (int)SubscriptionStatus.PastDue)
        {
            await _subscriptionsService.RenewSubscriptionAsync(subscription.Id, callerIsAdmin: true, cancellationToken);
        }

        return _mapper.Map<Payment>(payment);
    }

    public async Task<Payment> GetPaymentByIdAsync(
        Guid paymentId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var payment = await FetchPaymentOrThrow(paymentId, cancellationToken);

        // Discards the result - this call exists purely to enforce that the caller owns the
        // underlying subscription (or is an Admin), throwing SubscriptionAccessDeniedException
        // or SubscriptionNotFoundException otherwise.
        await _subscriptionsService.GetSubscriptionByIdAsync(payment.SubscriptionId, callerAccountGuid, callerIsAdmin, cancellationToken);

        return _mapper.Map<Payment>(payment);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsForSubscriptionAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        await _subscriptionsService.GetSubscriptionByIdAsync(subscriptionId, callerAccountGuid, callerIsAdmin, cancellationToken);

        var payments = await _paymentsRepository.FetchByConditionAsync(
            x => x.SubscriptionId == subscriptionId,
            cancellationToken);

        return _mapper.Map<IEnumerable<Payment>>(payments);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionsService.GetSubscriptionsForMemberAsync(
            memberAccountGuid,
            callerAccountGuid,
            callerIsAdmin,
            cancellationToken);
        var subscriptionIds = subscriptions.Select(x => x.Id).ToList();

        if (subscriptionIds.Count == 0)
        {
            return [];
        }

        var payments = await _paymentsRepository.FetchByConditionAsync(
            x => subscriptionIds.Contains(x.SubscriptionId),
            cancellationToken);

        return _mapper.Map<IEnumerable<Payment>>(payments);
    }

    public async Task<Payment> RefundPaymentAsync(
        Guid paymentId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(callerIsAdmin);

        var payment = await FetchPaymentOrThrow(paymentId, cancellationToken);

        payment.Status = (int)PaymentStatus.Refunded;

        _paymentsRepository.Update(payment);
        await _unitOfWork.SaveAsync(cancellationToken);

        return _mapper.Map<Payment>(payment);
    }

    private async Task<Models.Entities.Payment> FetchPaymentOrThrow(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = (await _paymentsRepository.FetchByConditionAsync(
            x => x.Id == paymentId,
            cancellationToken)).FirstOrDefault();

        if (payment is null)
        {
            throw new PaymentNotFoundException();
        }

        return payment;
    }

    private static void EnsureAdmin(bool callerIsAdmin)
    {
        if (!callerIsAdmin)
        {
            throw new SubscriptionAccessDeniedException("Only an Admin can record or refund payments");
        }
    }
}
