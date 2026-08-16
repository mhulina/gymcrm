using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Exceptions;
using GymCRM.BillingAPI.Models.Interface;
using GymCRM.BillingAPI.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using InsertSubscription = GymCRM.BillingAPI.Models.DTOs.InsertSubscription;
using SubscriptionEntity = GymCRM.BillingAPI.Models.Entities.Subscription;

namespace GymCRM.BillingAPI.Tests.Unit;

public class TestSubscriptionsService
{
    [Fact]
    public async Task GivenNullInsertSubscription_WhenCreatingSubscription_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService();

        // When
        Func<Task> act = () => service.CreateSubscriptionAsync(null!, callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenCreatingSubscription_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService();
        var insertSubscription = new InsertSubscription { MemberAccountGuid = Guid.NewGuid(), PlanType = (int)SubscriptionPlanType.Monthly };

        // When
        Func<Task> act = () => service.CreateSubscriptionAsync(insertSubscription, callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Theory]
    [InlineData(SubscriptionPlanType.Daily)]
    [InlineData(SubscriptionPlanType.Monthly)]
    [InlineData(SubscriptionPlanType.Yearly)]
    public async Task GivenValidInsertSubscription_WhenCreatingSubscription_ThenSubscriptionIsActiveWithCorrectRenewalDate(SubscriptionPlanType planType)
    {
        // Given
        var repositoryMock = new Mock<ISubscriptionsRepository>();
        var service = CreateSubscriptionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var memberAccountGuid = Guid.NewGuid();
        var insertSubscription = new InsertSubscription { MemberAccountGuid = memberAccountGuid, PlanType = (int)planType };
        var before = DateTime.UtcNow;

        // When
        var result = await service.CreateSubscriptionAsync(insertSubscription, callerIsAdmin: true);

        // Then
        result.MemberAccountGuid.Should().Be(memberAccountGuid);
        result.Status.Should().Be((int)SubscriptionStatus.Active);
        var expectedNextRenewal = planType switch
        {
            SubscriptionPlanType.Daily => before.AddDays(1),
            SubscriptionPlanType.Monthly => before.AddMonths(1),
            SubscriptionPlanType.Yearly => before.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(planType))
        };
        result.NextRenewalDate.Should().BeCloseTo(expectedNextRenewal, TimeSpan.FromSeconds(5));
        repositoryMock.Verify(x => x.Insert(It.Is<SubscriptionEntity>(s => s.MemberAccountGuid == memberAccountGuid && s.PlanType == (int)planType)), Times.Once);
    }

    [Fact]
    public async Task GivenNoMatchingSubscription_WhenGettingById_ThenSubscriptionNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.GetSubscriptionByIdAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Fact]
    public async Task GivenMatchingSubscription_WhenGettingById_ThenTheSubscriptionIsReturned()
    {
        // Given
        var subscription = CreateSubscription();
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(subscription).Object);

        // When
        var result = await service.GetSubscriptionByIdAsync(subscription.Id, subscription.MemberAccountGuid, callerIsAdmin: false);

        // Then
        result.Id.Should().Be(subscription.Id);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingById_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var subscription = CreateSubscription();
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(subscription).Object);

        // When
        Func<Task> act = () => service.GetSubscriptionByIdAsync(subscription.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenAdminCaller_WhenGettingAnotherMembersSubscriptionById_ThenItIsReturned()
    {
        // Given
        var subscription = CreateSubscription();
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(subscription).Object);

        // When
        var result = await service.GetSubscriptionByIdAsync(subscription.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Id.Should().Be(subscription.Id);
    }

    [Fact]
    public async Task GivenNoActiveSubscriptionForMember_WhenGettingActiveSubscription_ThenNullIsReturned()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var cancelledSubscription = CreateSubscription(memberAccountGuid: memberAccountGuid, status: SubscriptionStatus.Cancelled);
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(cancelledSubscription).Object);

        // When
        var result = await service.GetActiveSubscriptionForMemberAsync(memberAccountGuid, memberAccountGuid, callerIsAdmin: false);

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenActiveSubscriptionForMember_WhenGettingActiveSubscription_ThenItIsReturned()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var activeSubscription = CreateSubscription(memberAccountGuid: memberAccountGuid, status: SubscriptionStatus.Active);
        var otherMembersSubscription = CreateSubscription(status: SubscriptionStatus.Active);
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(activeSubscription, otherMembersSubscription).Object);

        // When
        var result = await service.GetActiveSubscriptionForMemberAsync(memberAccountGuid, memberAccountGuid, callerIsAdmin: false);

        // Then
        result.Should().NotBeNull();
        result!.Id.Should().Be(activeSubscription.Id);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingActiveSubscriptionForMember_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var service = CreateSubscriptionsService();

        // When
        Func<Task> act = () => service.GetActiveSubscriptionForMemberAsync(memberAccountGuid, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSubscriptionsForMember_WhenGettingSubscriptionsForMember_ThenAllOfThatMembersSubscriptionsAreReturned()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var active = CreateSubscription(memberAccountGuid: memberAccountGuid, status: SubscriptionStatus.Active);
        var cancelled = CreateSubscription(memberAccountGuid: memberAccountGuid, status: SubscriptionStatus.Cancelled);
        var otherMembersSubscription = CreateSubscription();
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(active, cancelled, otherMembersSubscription).Object);

        // When
        var result = await service.GetSubscriptionsForMemberAsync(memberAccountGuid, memberAccountGuid, callerIsAdmin: false);

        // Then
        result.Should().HaveCount(2).And.OnlyContain(x => x.MemberAccountGuid == memberAccountGuid);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingSubscriptionsForMember_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var service = CreateSubscriptionsService();

        // When
        Func<Task> act = () => service.GetSubscriptionsForMemberAsync(memberAccountGuid, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNoMatchingSubscription_WhenRenewing_ThenSubscriptionNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.RenewSubscriptionAsync(Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenRenewing_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService();

        // When
        Func<Task> act = () => service.RenewSubscriptionAsync(Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Cancelled)]
    [InlineData(SubscriptionStatus.Expired)]
    public async Task GivenCancelledOrExpiredSubscription_WhenRenewing_ThenSubscriptionNotRenewableExceptionIsThrown(SubscriptionStatus status)
    {
        // Given
        var subscription = CreateSubscription(status: status);
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(subscription).Object);

        // When
        Func<Task> act = () => service.RenewSubscriptionAsync(subscription.Id, callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotRenewableException>();
    }

    [Fact]
    public async Task GivenPastDueSubscription_WhenRenewing_ThenSubscriptionIsActiveWithExtendedRenewalDate()
    {
        // Given
        var subscription = CreateSubscription(status: SubscriptionStatus.PastDue, planType: SubscriptionPlanType.Monthly);
        var repositoryMock = CreateRepositoryMock(subscription);
        var service = CreateSubscriptionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var before = DateTime.UtcNow;

        // When
        var result = await service.RenewSubscriptionAsync(subscription.Id, callerIsAdmin: true);

        // Then
        result.Status.Should().Be((int)SubscriptionStatus.Active);
        result.NextRenewalDate.Should().BeCloseTo(before.AddMonths(1), TimeSpan.FromSeconds(5));
        repositoryMock.Verify(x => x.Update(It.Is<SubscriptionEntity>(s => s.Id == subscription.Id && s.Status == (int)SubscriptionStatus.Active)), Times.Once);
    }

    [Fact]
    public async Task GivenNoMatchingSubscription_WhenCancelling_ThenSubscriptionNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.CancelSubscriptionAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenCancelling_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var subscription = CreateSubscription(status: SubscriptionStatus.Active);
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock(subscription).Object);

        // When
        Func<Task> act = () => service.CancelSubscriptionAsync(subscription.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenExistingSubscription_WhenCancelling_ThenSubscriptionIsCancelledWithNoRenewalDate()
    {
        // Given
        var subscription = CreateSubscription(status: SubscriptionStatus.Active);
        var repositoryMock = CreateRepositoryMock(subscription);
        var service = CreateSubscriptionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.CancelSubscriptionAsync(subscription.Id, subscription.MemberAccountGuid, callerIsAdmin: false);

        // Then
        result.Status.Should().Be((int)SubscriptionStatus.Cancelled);
        result.NextRenewalDate.Should().BeNull();
        repositoryMock.Verify(x => x.Update(It.Is<SubscriptionEntity>(s => s.Id == subscription.Id && s.NextRenewalDate == null)), Times.Once);
    }

    [Fact]
    public async Task GivenAdminCaller_WhenCancellingAnotherMembersSubscription_ThenItSucceeds()
    {
        // Given
        var subscription = CreateSubscription(status: SubscriptionStatus.Active);
        var repositoryMock = CreateRepositoryMock(subscription);
        var service = CreateSubscriptionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.CancelSubscriptionAsync(subscription.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Status.Should().Be((int)SubscriptionStatus.Cancelled);
    }

    [Fact]
    public async Task GivenNoMatchingSubscription_WhenMarkingPastDue_ThenSubscriptionNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.MarkSubscriptionPastDueAsync(Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenMarkingPastDue_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateSubscriptionsService();

        // When
        Func<Task> act = () => service.MarkSubscriptionPastDueAsync(Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenActiveSubscription_WhenMarkingPastDue_ThenStatusIsPastDue()
    {
        // Given
        var subscription = CreateSubscription(status: SubscriptionStatus.Active);
        var repositoryMock = CreateRepositoryMock(subscription);
        var service = CreateSubscriptionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.MarkSubscriptionPastDueAsync(subscription.Id, callerIsAdmin: true);

        // Then
        result.Status.Should().Be((int)SubscriptionStatus.PastDue);
        repositoryMock.Verify(x => x.Update(It.Is<SubscriptionEntity>(s => s.Id == subscription.Id && s.Status == (int)SubscriptionStatus.PastDue)), Times.Once);
    }

    private static SubscriptionEntity CreateSubscription(
        Guid? memberAccountGuid = null,
        SubscriptionStatus status = SubscriptionStatus.Active,
        SubscriptionPlanType planType = SubscriptionPlanType.Monthly)
    {
        var now = DateTime.UtcNow;

        return new SubscriptionEntity
        {
            Id = Guid.NewGuid(),
            MemberAccountGuid = memberAccountGuid ?? Guid.NewGuid(),
            PlanType = (int)planType,
            Status = (int)status,
            StartDate = now,
            NextRenewalDate = now.AddMonths(1),
            DateCreated = now,
            DateModified = now
        };
    }

    // Backs FetchByConditionAsync with an in-memory list and compiles/applies the predicate
    // expression against it.
    private static Mock<ISubscriptionsRepository> CreateRepositoryMock(params SubscriptionEntity[] subscriptions)
    {
        var backingList = subscriptions.ToList();
        var repositoryMock = new Mock<ISubscriptionsRepository>();
        repositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<SubscriptionEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SubscriptionEntity, bool>> expression, CancellationToken _) =>
                backingList.Where(expression.Compile()).ToList());

        return repositoryMock;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool saveResult)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);

        return unitOfWorkMock;
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(BillingModule.ConfigureBillingMappings);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static SubscriptionsService CreateSubscriptionsService(
        ISubscriptionsRepository? repository = null,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null) =>
        new(
            repository ?? Mock.Of<ISubscriptionsRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            mapper ?? CreateMapper());
}
