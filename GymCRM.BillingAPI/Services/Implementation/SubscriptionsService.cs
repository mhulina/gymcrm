using AutoMapper;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.DTOs;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Exceptions;
using GymCRM.BillingAPI.Models.Interface;
using GymCRM.BillingAPI.Services.Interface;

namespace GymCRM.BillingAPI.Services.Implementation;

public class SubscriptionsService : ISubscriptionsService
{
    private readonly ISubscriptionsRepository _subscriptionsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SubscriptionsService(
        ISubscriptionsRepository subscriptionsRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _subscriptionsRepository = subscriptionsRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Subscription> CreateSubscriptionAsync(
        InsertSubscription insertSubscription,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        if (insertSubscription is null)
        {
            throw new ArgumentNullException(nameof(insertSubscription));
        }

        EnsureAdmin(callerIsAdmin);

        var now = DateTime.UtcNow;
        var subscription = new Models.Entities.Subscription
        {
            Id = Guid.NewGuid(),
            MemberAccountGuid = insertSubscription.MemberAccountGuid,
            PlanType = insertSubscription.PlanType,
            Status = (int)SubscriptionStatus.Active,
            StartDate = now,
            NextRenewalDate = ComputeNextRenewalDate(now, (SubscriptionPlanType)insertSubscription.PlanType),
            DateCreated = now,
            DateModified = now
        };

        _subscriptionsRepository.Insert(subscription);
        await _unitOfWork.SaveAsync(cancellationToken);

        return _mapper.Map<Subscription>(subscription);
    }

    public async Task<Subscription> GetSubscriptionByIdAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var subscription = await FetchSubscriptionOrThrow(subscriptionId, cancellationToken);

        EnsureSelfOrAdmin(subscription.MemberAccountGuid, callerAccountGuid, callerIsAdmin);

        return _mapper.Map<Subscription>(subscription);
    }

    public async Task<Subscription?> GetActiveSubscriptionForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(memberAccountGuid, callerAccountGuid, callerIsAdmin);

        var subscription = (await _subscriptionsRepository.FetchByConditionAsync(
            x => x.MemberAccountGuid == memberAccountGuid && x.Status == (int)SubscriptionStatus.Active,
            cancellationToken)).FirstOrDefault();

        return subscription is null ? null : _mapper.Map<Subscription>(subscription);
    }

    public async Task<IEnumerable<Subscription>> GetSubscriptionsForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        EnsureSelfOrAdmin(memberAccountGuid, callerAccountGuid, callerIsAdmin);

        var subscriptions = await _subscriptionsRepository.FetchByConditionAsync(
            x => x.MemberAccountGuid == memberAccountGuid,
            cancellationToken);

        return _mapper.Map<IEnumerable<Subscription>>(subscriptions);
    }

    public async Task<Subscription> RenewSubscriptionAsync(
        Guid subscriptionId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(callerIsAdmin);

        var subscription = await FetchSubscriptionOrThrow(subscriptionId, cancellationToken);

        if (subscription.Status is (int)SubscriptionStatus.Cancelled or (int)SubscriptionStatus.Expired)
        {
            throw new SubscriptionNotRenewableException();
        }

        var now = DateTime.UtcNow;
        subscription.Status = (int)SubscriptionStatus.Active;
        subscription.NextRenewalDate = ComputeNextRenewalDate(now, (SubscriptionPlanType)subscription.PlanType);
        subscription.DateModified = now;

        _subscriptionsRepository.Update(subscription);
        await _unitOfWork.SaveAsync(cancellationToken);

        return _mapper.Map<Subscription>(subscription);
    }

    public async Task<Subscription> CancelSubscriptionAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        var subscription = await FetchSubscriptionOrThrow(subscriptionId, cancellationToken);

        EnsureSelfOrAdmin(subscription.MemberAccountGuid, callerAccountGuid, callerIsAdmin);

        subscription.Status = (int)SubscriptionStatus.Cancelled;
        subscription.NextRenewalDate = null;
        subscription.DateModified = DateTime.UtcNow;

        _subscriptionsRepository.Update(subscription);
        await _unitOfWork.SaveAsync(cancellationToken);

        return _mapper.Map<Subscription>(subscription);
    }

    public async Task<Subscription> MarkSubscriptionPastDueAsync(
        Guid subscriptionId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(callerIsAdmin);

        var subscription = await FetchSubscriptionOrThrow(subscriptionId, cancellationToken);

        subscription.Status = (int)SubscriptionStatus.PastDue;
        subscription.DateModified = DateTime.UtcNow;

        _subscriptionsRepository.Update(subscription);
        await _unitOfWork.SaveAsync(cancellationToken);

        return _mapper.Map<Subscription>(subscription);
    }

    private async Task<Models.Entities.Subscription> FetchSubscriptionOrThrow(Guid subscriptionId, CancellationToken cancellationToken)
    {
        var subscription = (await _subscriptionsRepository.FetchByConditionAsync(
            x => x.Id == subscriptionId,
            cancellationToken)).FirstOrDefault();

        if (subscription is null)
        {
            throw new SubscriptionNotFoundException();
        }

        return subscription;
    }

    private static void EnsureAdmin(bool callerIsAdmin)
    {
        if (!callerIsAdmin)
        {
            throw new SubscriptionAccessDeniedException("Only an Admin can perform this action");
        }
    }

    private static void EnsureSelfOrAdmin(Guid memberAccountGuid, Guid callerAccountGuid, bool callerIsAdmin)
    {
        if (callerIsAdmin || memberAccountGuid == callerAccountGuid)
        {
            return;
        }

        throw new SubscriptionAccessDeniedException();
    }

    private static DateTime ComputeNextRenewalDate(DateTime from, SubscriptionPlanType planType) => planType switch
    {
        SubscriptionPlanType.Daily => from.AddDays(1),
        SubscriptionPlanType.Monthly => from.AddMonths(1),
        SubscriptionPlanType.Yearly => from.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(planType), planType, "Unknown subscription plan type")
    };
}
