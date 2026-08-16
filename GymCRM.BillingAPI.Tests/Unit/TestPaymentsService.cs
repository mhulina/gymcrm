using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.BillingAPI.Infrastructure.Interface;
using GymCRM.BillingAPI.Models.Enums;
using GymCRM.BillingAPI.Models.Exceptions;
using GymCRM.BillingAPI.Models.Interface;
using GymCRM.BillingAPI.Services.Implementation;
using GymCRM.BillingAPI.Services.Interface;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using InsertPayment = GymCRM.BillingAPI.Models.DTOs.InsertPayment;
using PaymentEntity = GymCRM.BillingAPI.Models.Entities.Payment;
using SubscriptionDto = GymCRM.BillingAPI.Models.DTOs.Subscription;

namespace GymCRM.BillingAPI.Tests.Unit;

public class TestPaymentsService
{
    [Fact]
    public async Task GivenNullInsertPayment_WhenRecordingPayment_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreatePaymentsService();

        // When
        Func<Task> act = () => service.RecordPaymentAsync(null!, callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenRecordingPayment_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreatePaymentsService();
        var insertPayment = new InsertPayment { SubscriptionId = Guid.NewGuid(), Amount = 10m, Method = (int)PaymentMethod.Card, Status = (int)PaymentStatus.Succeeded };

        // When
        Func<Task> act = () => service.RecordPaymentAsync(insertPayment, callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNoMatchingSubscription_WhenRecordingPayment_ThenSubscriptionNotFoundExceptionIsThrown()
    {
        // Given
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionByIdAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionNotFoundException());
        var service = CreatePaymentsService(subscriptionsService: subscriptionsServiceMock.Object);
        var insertPayment = new InsertPayment { SubscriptionId = Guid.NewGuid(), Amount = 10m, Method = (int)PaymentMethod.Card, Status = (int)PaymentStatus.Succeeded };

        // When
        Func<Task> act = () => service.RecordPaymentAsync(insertPayment, callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<SubscriptionNotFoundException>();
    }

    [Fact]
    public async Task GivenSucceededPaymentOnActiveSubscription_WhenRecordingPayment_ThenPaymentIsInsertedAndSubscriptionIsNotRenewed()
    {
        // Given
        var subscription = CreateSubscriptionDto(SubscriptionStatus.Active);
        var subscriptionsServiceMock = CreateSubscriptionsServiceMock(subscription);
        var repositoryMock = new Mock<IPaymentsRepository>();
        var service = CreatePaymentsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            subscriptionsService: subscriptionsServiceMock.Object);
        var insertPayment = new InsertPayment { SubscriptionId = subscription.Id, Amount = 29.99m, Method = (int)PaymentMethod.Card, Status = (int)PaymentStatus.Succeeded };

        // When
        var result = await service.RecordPaymentAsync(insertPayment, callerIsAdmin: true);

        // Then
        result.Amount.Should().Be(29.99m);
        result.Status.Should().Be((int)PaymentStatus.Succeeded);
        repositoryMock.Verify(x => x.Insert(It.Is<PaymentEntity>(p => p.SubscriptionId == subscription.Id && p.Amount == 29.99m)), Times.Once);
        subscriptionsServiceMock.Verify(
            x => x.RenewSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenSucceededPaymentOnPastDueSubscription_WhenRecordingPayment_ThenSubscriptionIsRenewed()
    {
        // Given
        var subscription = CreateSubscriptionDto(SubscriptionStatus.PastDue);
        var subscriptionsServiceMock = CreateSubscriptionsServiceMock(subscription);
        var service = CreatePaymentsService(
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            subscriptionsService: subscriptionsServiceMock.Object);
        var insertPayment = new InsertPayment { SubscriptionId = subscription.Id, Amount = 29.99m, Method = (int)PaymentMethod.Card, Status = (int)PaymentStatus.Succeeded };

        // When
        await service.RecordPaymentAsync(insertPayment, callerIsAdmin: true);

        // Then
        subscriptionsServiceMock.Verify(
            x => x.RenewSubscriptionAsync(subscription.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenFailedPaymentOnPastDueSubscription_WhenRecordingPayment_ThenSubscriptionIsNotRenewed()
    {
        // Given
        var subscription = CreateSubscriptionDto(SubscriptionStatus.PastDue);
        var subscriptionsServiceMock = CreateSubscriptionsServiceMock(subscription);
        var service = CreatePaymentsService(
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            subscriptionsService: subscriptionsServiceMock.Object);
        var insertPayment = new InsertPayment { SubscriptionId = subscription.Id, Amount = 29.99m, Method = (int)PaymentMethod.Card, Status = (int)PaymentStatus.Failed };

        // When
        await service.RecordPaymentAsync(insertPayment, callerIsAdmin: true);

        // Then
        subscriptionsServiceMock.Verify(
            x => x.RenewSubscriptionAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenNoMatchingPayment_WhenGettingById_ThenPaymentNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreatePaymentsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.GetPaymentByIdAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task GivenMatchingPayment_WhenGettingById_ThenThePaymentIsReturned()
    {
        // Given
        var payment = CreatePayment();
        var service = CreatePaymentsService(repository: CreateRepositoryMock(payment).Object);

        // When
        var result = await service.GetPaymentByIdAsync(payment.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingPaymentById_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var payment = CreatePayment();
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionByIdAsync(payment.SubscriptionId, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionAccessDeniedException());
        var service = CreatePaymentsService(
            repository: CreateRepositoryMock(payment).Object,
            subscriptionsService: subscriptionsServiceMock.Object);

        // When
        Func<Task> act = () => service.GetPaymentByIdAsync(payment.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenAdminCaller_WhenGettingAnotherMembersPaymentById_ThenItIsReturned()
    {
        // Given
        var payment = CreatePayment();
        var service = CreatePaymentsService(repository: CreateRepositoryMock(payment).Object);

        // When
        var result = await service.GetPaymentByIdAsync(payment.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Id.Should().Be(payment.Id);
    }

    [Fact]
    public async Task GivenPaymentsForDifferentSubscriptions_WhenGettingPaymentsForSubscription_ThenOnlyMatchingPaymentsAreReturned()
    {
        // Given
        var subscriptionId = Guid.NewGuid();
        var matchingPayment = CreatePayment(subscriptionId: subscriptionId);
        var otherPayment = CreatePayment();
        var service = CreatePaymentsService(repository: CreateRepositoryMock(matchingPayment, otherPayment).Object);

        // When
        var result = await service.GetPaymentsForSubscriptionAsync(subscriptionId, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().ContainSingle(x => x.Id == matchingPayment.Id);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingPaymentsForSubscription_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var subscriptionId = Guid.NewGuid();
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionByIdAsync(subscriptionId, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionAccessDeniedException());
        var service = CreatePaymentsService(subscriptionsService: subscriptionsServiceMock.Object);

        // When
        Func<Task> act = () => service.GetPaymentsForSubscriptionAsync(subscriptionId, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenMemberWithNoSubscriptions_WhenGettingPaymentsForMember_ThenEmptyListIsReturned()
    {
        // Given
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionsForMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var service = CreatePaymentsService(subscriptionsService: subscriptionsServiceMock.Object);

        // When
        var result = await service.GetPaymentsForMemberAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenMemberWithSubscriptions_WhenGettingPaymentsForMember_ThenOnlyThatMembersPaymentsAreReturned()
    {
        // Given
        var memberSubscription = CreateSubscriptionDto(SubscriptionStatus.Active);
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionsForMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([memberSubscription]);
        var matchingPayment = CreatePayment(subscriptionId: memberSubscription.Id);
        var otherPayment = CreatePayment();
        var service = CreatePaymentsService(
            repository: CreateRepositoryMock(matchingPayment, otherPayment).Object,
            subscriptionsService: subscriptionsServiceMock.Object);

        // When
        var result = await service.GetPaymentsForMemberAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().ContainSingle(x => x.Id == matchingPayment.Id);
    }

    [Fact]
    public async Task GivenNonOwnerNonAdminCaller_WhenGettingPaymentsForMember_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var memberAccountGuid = Guid.NewGuid();
        var subscriptionsServiceMock = new Mock<ISubscriptionsService>();
        subscriptionsServiceMock
            .Setup(x => x.GetSubscriptionsForMemberAsync(memberAccountGuid, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionAccessDeniedException());
        var service = CreatePaymentsService(subscriptionsService: subscriptionsServiceMock.Object);

        // When
        Func<Task> act = () => service.GetPaymentsForMemberAsync(memberAccountGuid, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNoMatchingPayment_WhenRefunding_ThenPaymentNotFoundExceptionIsThrown()
    {
        // Given
        var service = CreatePaymentsService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.RefundPaymentAsync(Guid.NewGuid(), callerIsAdmin: true);

        // Then
        await act.Should().ThrowAsync<PaymentNotFoundException>();
    }

    [Fact]
    public async Task GivenNonAdminCaller_WhenRefunding_ThenSubscriptionAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreatePaymentsService();

        // When
        Func<Task> act = () => service.RefundPaymentAsync(Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<SubscriptionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenExistingPayment_WhenRefunding_ThenPaymentStatusIsRefunded()
    {
        // Given
        var payment = CreatePayment();
        var repositoryMock = CreateRepositoryMock(payment);
        var service = CreatePaymentsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.RefundPaymentAsync(payment.Id, callerIsAdmin: true);

        // Then
        result.Status.Should().Be((int)PaymentStatus.Refunded);
        repositoryMock.Verify(x => x.Update(It.Is<PaymentEntity>(p => p.Id == payment.Id && p.Status == (int)PaymentStatus.Refunded)), Times.Once);
    }

    private static PaymentEntity CreatePayment(Guid? subscriptionId = null) => new()
    {
        Id = Guid.NewGuid(),
        SubscriptionId = subscriptionId ?? Guid.NewGuid(),
        Amount = 29.99m,
        Method = (int)PaymentMethod.Card,
        Status = (int)PaymentStatus.Succeeded,
        PaidAt = DateTime.UtcNow,
        DateCreated = DateTime.UtcNow
    };

    private static SubscriptionDto CreateSubscriptionDto(SubscriptionStatus status) => new()
    {
        Id = Guid.NewGuid(),
        MemberAccountGuid = Guid.NewGuid(),
        PlanType = (int)SubscriptionPlanType.Monthly,
        Status = (int)status,
        StartDate = DateTime.UtcNow,
        NextRenewalDate = DateTime.UtcNow.AddMonths(1),
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow
    };

    private static Mock<ISubscriptionsService> CreateSubscriptionsServiceMock(SubscriptionDto subscription)
    {
        var mock = new Mock<ISubscriptionsService>();
        mock
            .Setup(x => x.GetSubscriptionByIdAsync(subscription.Id, It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        return mock;
    }

    // Backs FetchByConditionAsync with an in-memory list and compiles/applies the predicate
    // expression against it.
    private static Mock<IPaymentsRepository> CreateRepositoryMock(params PaymentEntity[] payments)
    {
        var backingList = payments.ToList();
        var repositoryMock = new Mock<IPaymentsRepository>();
        repositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<PaymentEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PaymentEntity, bool>> expression, CancellationToken _) =>
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

    private static PaymentsService CreatePaymentsService(
        IPaymentsRepository? repository = null,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null,
        ISubscriptionsService? subscriptionsService = null) =>
        new(
            repository ?? Mock.Of<IPaymentsRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            mapper ?? CreateMapper(),
            subscriptionsService ?? Mock.Of<ISubscriptionsService>());
}
