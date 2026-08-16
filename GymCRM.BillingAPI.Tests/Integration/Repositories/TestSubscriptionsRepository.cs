using FluentAssertions;
using GymCRM.BillingAPI.Infrastructure.Implementation;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Entities;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Interface;

namespace GymCRM.BillingAPI.Tests.Integration.Repositories;

public class TestSubscriptionsRepository : TestBase
{
    private readonly ISubscriptionsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestSubscriptionsRepository()
    {
        _repository = new SubscriptionsRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenValidSubscription_WhenInserting_ThenTheSubscriptionIsSaved()
    {
        // Given
        var subscription = CreateSubscription();

        // When
        _repository.Insert(subscription);
        var result = await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var fetched = await _repository.FetchByConditionAsync(x => x.Id == subscription.Id, CancellationToken.None);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public async Task GivenSubscriptionsForDifferentMembers_WhenFetchingByCondition_ThenOnlyMatchingSubscriptionsAreReturned()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var targetSubscription = CreateSubscription(memberAccountGuid: memberAccountGuid);
        var otherSubscription = CreateSubscription();
        _repository.Insert(targetSubscription);
        _repository.Insert(otherSubscription);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(
            x => x.MemberAccountGuid == memberAccountGuid,
            CancellationToken.None)).ToList();

        // Then
        result.Should().ContainSingle(x => x.Id == targetSubscription.Id);
    }

    [Fact]
    public async Task GivenExistingSubscription_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var subscription = CreateSubscription();
        _repository.Insert(subscription);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // When
        subscription.Status = (int)SubscriptionStatus.Cancelled;
        subscription.NextRenewalDate = null;
        _repository.Update(subscription);
        var result = await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = (await _repository.FetchByConditionAsync(x => x.Id == subscription.Id, CancellationToken.None)).First();
        updated.Status.Should().Be((int)SubscriptionStatus.Cancelled);
        updated.NextRenewalDate.Should().BeNull();
    }

    [Fact]
    public async Task GivenSubscriptionWithPayments_WhenFetchingByCondition_ThenPaymentsAreIncluded()
    {
        // Given
        var subscription = CreateSubscription();
        _repository.Insert(subscription);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        var paymentsRepository = new PaymentsRepository(_context);
        paymentsRepository.Insert(new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscription.Id,
            Amount = 29.99m,
            Method = (int)PaymentMethod.Card,
            Status = (int)PaymentStatus.Succeeded,
            PaidAt = DateTime.UtcNow,
            DateCreated = DateTime.UtcNow
        });
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(x => x.Id == subscription.Id, CancellationToken.None)).First();

        // Then
        result.Payments.Should().ContainSingle(x => x.Amount == 29.99m);
    }

    private static Subscription CreateSubscription(Guid? memberAccountGuid = null)
    {
        var now = DateTime.UtcNow;

        return new Subscription
        {
            Id = Guid.NewGuid(),
            MemberAccountGuid = memberAccountGuid ?? Guid.NewGuid(),
            PlanType = (int)SubscriptionPlanType.Monthly,
            Status = (int)SubscriptionStatus.Active,
            StartDate = now,
            NextRenewalDate = now.AddMonths(1),
            DateCreated = now,
            DateModified = now
        };
    }
}
