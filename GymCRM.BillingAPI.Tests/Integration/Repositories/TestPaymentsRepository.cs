using FluentAssertions;
using GymCRM.BillingAPI.Infrastructure.Implementation;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Entities;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Interface;

namespace GymCRM.BillingAPI.Tests.Integration.Repositories;

public class TestPaymentsRepository : TestBase
{
    private readonly IPaymentsRepository _repository;
    private readonly ISubscriptionsRepository _subscriptionsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TestPaymentsRepository()
    {
        _repository = new PaymentsRepository(_context);
        _subscriptionsRepository = new SubscriptionsRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenValidPayment_WhenInserting_ThenThePaymentIsSaved()
    {
        // Given
        var subscription = await InsertSubscription();
        var payment = CreatePayment(subscription.Id);

        // When
        _repository.Insert(payment);
        var result = await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var fetched = await _repository.FetchByConditionAsync(x => x.Id == payment.Id, CancellationToken.None);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public async Task GivenPaymentsForDifferentSubscriptions_WhenFetchingByCondition_ThenOnlyMatchingPaymentsAreReturned()
    {
        // Given
        var targetSubscription = await InsertSubscription();
        var otherSubscription = await InsertSubscription();
        var targetPayment = CreatePayment(targetSubscription.Id);
        _repository.Insert(targetPayment);
        _repository.Insert(CreatePayment(otherSubscription.Id));
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(
            x => x.SubscriptionId == targetSubscription.Id,
            CancellationToken.None)).ToList();

        // Then
        result.Should().ContainSingle(x => x.Id == targetPayment.Id);
    }

    [Fact]
    public async Task GivenExistingPayment_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var subscription = await InsertSubscription();
        var payment = CreatePayment(subscription.Id);
        _repository.Insert(payment);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        // When
        payment.Status = (int)PaymentStatus.Refunded;
        _repository.Update(payment);
        var result = await _unitOfWork.SaveAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = (await _repository.FetchByConditionAsync(x => x.Id == payment.Id, CancellationToken.None)).First();
        updated.Status.Should().Be((int)PaymentStatus.Refunded);
    }

    private async Task<Subscription> InsertSubscription()
    {
        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            MemberAccountGuid = Guid.NewGuid(),
            PlanType = (int)SubscriptionPlanType.Monthly,
            Status = (int)SubscriptionStatus.Active,
            StartDate = now,
            NextRenewalDate = now.AddMonths(1),
            DateCreated = now,
            DateModified = now
        };

        _subscriptionsRepository.Insert(subscription);
        await _unitOfWork.SaveAsync(CancellationToken.None);

        return subscription;
    }

    private static Payment CreatePayment(Guid subscriptionId)
    {
        var now = DateTime.UtcNow;

        return new Payment
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Amount = 29.99m,
            Method = (int)PaymentMethod.Card,
            Status = (int)PaymentStatus.Succeeded,
            PaidAt = now,
            DateCreated = now
        };
    }
}
