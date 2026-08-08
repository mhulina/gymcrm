using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using InsertTrainingSession = GymCRM.SchedulingAPI.Models.DTOs.InsertTrainingSession;
using TrainingSessionDto = GymCRM.SchedulingAPI.Models.DTOs.TrainingSession;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestTrainingSessionsService
{
    [Fact]
    public async Task GivenTrainingSessionsExist_WhenGettingAll_ThenMappedTrainingSessionsAreReturned()
    {
        // Given
        var session = CreateTrainingSession();
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        var result = await service.GetAllAsync();

        // Then
        result.Should().ContainSingle(t => t.Id == session.Id);
    }

    [Fact]
    public async Task GivenSessionsWithVariousStatuses_WhenGettingCancelledSessions_ThenOnlyCancelledAreReturned()
    {
        // Given
        var cancelled = CreateTrainingSession(status: TrainingSessionStatus.Cancelled);
        var booked = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(cancelled, booked).Object);

        // When
        var result = await service.GetCancelledTrainingSessionsAsync();

        // Then
        result.Should().ContainSingle(t => t.Id == cancelled.Id);
    }

    [Fact]
    public async Task GivenSessionsWithVariousStatuses_WhenGettingPendingSessions_ThenOnlyBookedAreReturned()
    {
        // Given
        var booked = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var completed = CreateTrainingSession(status: TrainingSessionStatus.Completed);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(booked, completed).Object);

        // When
        var result = await service.GetPendingTrainingSessionsAsync();

        // Then
        result.Should().ContainSingle(t => t.Id == booked.Id);
    }

    [Fact]
    public async Task GivenSessionsWithVariousStatuses_WhenGettingCompletedSessions_ThenOnlyCompletedAreReturned()
    {
        // Given
        var completed = CreateTrainingSession(status: TrainingSessionStatus.Completed);
        var booked = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(completed, booked).Object);

        // When
        var result = await service.GetCompletedTrainingSessionsAsync();

        // Then
        result.Should().ContainSingle(t => t.Id == completed.Id);
    }

    [Fact]
    public async Task GivenClientId_WhenGettingTrainingSessionsForClientId_ThenOnlyThatClientsSessionsAreReturned()
    {
        // Given
        var clientId = Guid.NewGuid();
        var ownSession = CreateTrainingSession(clientId: clientId);
        var otherSession = CreateTrainingSession();
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(ownSession, otherSession).Object);

        // When
        var result = await service.GetTrainingSessionsForClientIdAsync(clientId);

        // Then
        result.Should().ContainSingle(t => t.Id == ownSession.Id);
    }

    [Fact]
    public async Task GivenTrainerId_WhenGettingTrainingSessionsForTrainerId_ThenOnlyThatTrainersSessionsAreReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var ownSession = CreateTrainingSession(trainerId: trainerId);
        var otherSession = CreateTrainingSession();
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(ownSession, otherSession).Object);

        // When
        var result = await service.GetTrainingSessionsForTrainerIdAsync(trainerId);

        // Then
        result.Should().ContainSingle(t => t.Id == ownSession.Id);
    }

    [Fact]
    public async Task GivenTrainerIdAndMonth_WhenGettingTrainingSessionsForTrainerIdInMonth_ThenOnlyMatchingSessionsAreReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var matchingSession = CreateTrainingSession(trainerId: trainerId, startTime: new DateTime(2024, 6, 10, 9, 0, 0, DateTimeKind.Utc));
        var otherMonthSession = CreateTrainingSession(trainerId: trainerId, startTime: new DateTime(2024, 7, 10, 9, 0, 0, DateTimeKind.Utc));
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(matchingSession, otherMonthSession).Object);

        // When
        var result = await service.GetTrainingSessionsForTrainerIdInMonthAsync(trainerId, 6);

        // Then
        result.Should().ContainSingle(t => t.Id == matchingSession.Id);
    }

    [Fact]
    public async Task GivenNullInsertTrainingSession_WhenInsertingTrainingSession_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainingSessionsService();

        // When
        Func<Task> act = () => service.InsertTrainingSessionAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidInsertTrainingSession_WhenInsertingTrainingSession_ThenSessionIsAddedAsRequested()
    {
        // Given
        var repositoryMock = new Mock<ITrainingSessionsRepository>();
        var service = CreateTrainingSessionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var insert = new InsertTrainingSession
        {
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Description = "First session"
        };

        // When
        var result = await service.InsertTrainingSessionAsync(insert);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Add(It.Is<TrainingSession>(t =>
            t.TrainerId == insert.TrainerId
            && t.ClientId == insert.ClientId
            && t.Status == (int)TrainingSessionStatus.Requested)), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenDeletingTrainingSession_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainingSessionsService();

        // When
        Func<Task> act = () => service.DeleteTrainingSessionAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidGuid_WhenDeletingTrainingSession_ThenSessionIsRemoved()
    {
        // Given
        var repositoryMock = new Mock<ITrainingSessionsRepository>();
        var service = CreateTrainingSessionsService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var sessionId = Guid.NewGuid();

        // When
        var result = await service.DeleteTrainingSessionAsync(sessionId);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Remove(It.Is<TrainingSession>(t => t.Id == sessionId)), Times.Once);
    }

    [Fact]
    public async Task GivenNullUpdatedTrainingSession_WhenUpdatingTrainingSession_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainingSessionsService();

        // When
        Func<Task> act = () => service.UpdateTrainingSessionAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidUpdatedTrainingSession_WhenUpdatingTrainingSession_ThenSessionIsUpdated()
    {
        // Given
        var repositoryMock = new Mock<ITrainingSessionsRepository>();
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            mapper: CreateMapper());
        var dto = new TrainingSessionDto
        {
            Id = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(1),
            Status = (int)TrainingSessionStatus.Reschedule
        };

        // When
        var result = await service.UpdateTrainingSessionAsync(dto);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t => t.Id == dto.Id && t.Status == (int)TrainingSessionStatus.Reschedule)), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentId_WhenGettingTrainingSessionById_ThenNullIsReturned()
    {
        // Given
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock().Object);

        // When
        var result = await service.GetTrainingSessionByIdAsync(Guid.NewGuid());

        // Then
        result.Should().BeNull();
    }

    [Fact]
    public async Task GivenExistingId_WhenGettingTrainingSessionById_ThenMatchingSessionIsReturned()
    {
        // Given
        var session = CreateTrainingSession();
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        var result = await service.GetTrainingSessionByIdAsync(session.Id);

        // Then
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
    }

    [Fact]
    public async Task GivenNonExistentId_WhenAcceptingTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock().Object);

        // When
        var result = await service.AcceptTrainingSessionAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenAcceptingTrainingSession_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        Func<Task> act = () => service.AcceptTrainingSessionAsync(session.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainingSessionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSessionNotInRequestedStatus_WhenAcceptingTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        var result = await service.AcceptTrainingSessionAsync(session.Id, session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOwningTrainerCaller_WhenAcceptingTrainingSession_ThenSessionIsBooked()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.AcceptTrainingSessionAsync(session.Id, session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id && t.Status == (int)TrainingSessionStatus.Booked)), Times.Once);
    }

    [Fact]
    public async Task GivenAdminCaller_WhenAcceptingAnotherTrainersSession_ThenSessionIsBooked()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.AcceptTrainingSessionAsync(session.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id && t.Status == (int)TrainingSessionStatus.Booked)), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentId_WhenDecliningTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock().Object);

        // When
        var result = await service.DeclineTrainingSessionAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenDecliningTrainingSession_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        Func<Task> act = () => service.DeclineTrainingSessionAsync(session.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainingSessionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSessionNotInRequestedStatus_WhenDecliningTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);

        // When
        var result = await service.DeclineTrainingSessionAsync(session.Id, session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOwningTrainerCaller_WhenDecliningTrainingSession_ThenSessionIsCancelled()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.DeclineTrainingSessionAsync(session.Id, session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id && t.Status == (int)TrainingSessionStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task GivenAdminCaller_WhenDecliningAnotherTrainersSession_ThenSessionIsCancelled()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.DeclineTrainingSessionAsync(session.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id && t.Status == (int)TrainingSessionStatus.Cancelled)), Times.Once);
    }

    [Fact]
    public async Task GivenNonExistentId_WhenReschedulingTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock().Object);
        var newStart = DateTime.UtcNow.AddDays(1);

        // When
        var result = await service.RescheduleTrainingSessionAsync(
            Guid.NewGuid(), newStart, newStart.AddHours(1), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenReschedulingTrainingSession_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);
        var newStart = DateTime.UtcNow.AddDays(1);

        // When
        Func<Task> act = () => service.RescheduleTrainingSessionAsync(
            session.Id, newStart, newStart.AddHours(1), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainingSessionAccessDeniedException>();
    }

    [Fact]
    public async Task GivenSessionNotInRequestedStatus_WhenReschedulingTrainingSession_ThenFalseIsReturned()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Booked);
        var service = CreateTrainingSessionsService(repository: CreateRepositoryMock(session).Object);
        var newStart = DateTime.UtcNow.AddDays(1);

        // When
        var result = await service.RescheduleTrainingSessionAsync(
            session.Id, newStart, newStart.AddHours(1), session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenOwningTrainerCaller_WhenReschedulingTrainingSession_ThenSessionIsUpdatedAndBooked()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var newStart = DateTime.UtcNow.AddDays(1);
        var newEnd = newStart.AddHours(1);

        // When
        var result = await service.RescheduleTrainingSessionAsync(
            session.Id, newStart, newEnd, session.TrainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id
            && t.StartTime == newStart
            && t.EndTime == newEnd
            && t.Status == (int)TrainingSessionStatus.Booked)), Times.Once);
    }

    [Fact]
    public async Task GivenAdminCaller_WhenReschedulingAnotherTrainersSession_ThenSessionIsUpdatedAndBooked()
    {
        // Given
        var session = CreateTrainingSession(status: TrainingSessionStatus.Requested);
        var repositoryMock = CreateRepositoryMock(session);
        var service = CreateTrainingSessionsService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var newStart = DateTime.UtcNow.AddDays(1);
        var newEnd = newStart.AddHours(1);

        // When
        var result = await service.RescheduleTrainingSessionAsync(
            session.Id, newStart, newEnd, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TrainingSession>(t =>
            t.Id == session.Id
            && t.StartTime == newStart
            && t.EndTime == newEnd
            && t.Status == (int)TrainingSessionStatus.Booked)), Times.Once);
    }

    private static TrainingSession CreateTrainingSession(
        Guid? trainerId = null,
        Guid? clientId = null,
        TrainingSessionStatus status = TrainingSessionStatus.Booked,
        DateTime? startTime = null) => new()
    {
        Id = Guid.NewGuid(),
        TrainerId = trainerId ?? Guid.NewGuid(),
        ClientId = clientId ?? Guid.NewGuid(),
        Status = (int)status,
        StartTime = startTime ?? DateTime.UtcNow,
        EndTime = (startTime ?? DateTime.UtcNow).AddHours(1),
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow
    };

    // Backs FetchAllAsync/FetchByConditionAsync with an in-memory list and compiles/applies the
    // predicate expression against it.
    private static Mock<ITrainingSessionsRepository> CreateRepositoryMock(params TrainingSession[] sessions)
    {
        var backingList = sessions.ToList();
        var repositoryMock = new Mock<ITrainingSessionsRepository>();
        repositoryMock.Setup(x => x.FetchAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(backingList);
        repositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<TrainingSession, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TrainingSession, bool>> expression, CancellationToken _) =>
                backingList.Where(expression.Compile()).ToList());

        return repositoryMock;
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock(bool saveResult)
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(saveResult);

        return unitOfWorkMock;
    }

    private static IMapper CreateMapper()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAutoMapper(SchedulingModule.ConfigureSchedulingMappings);

        return services.BuildServiceProvider().GetRequiredService<IMapper>();
    }

    private static TrainingSessionsService CreateTrainingSessionsService(
        ITrainingSessionsRepository? repository = null,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null) =>
        new(
            repository ?? Mock.Of<ITrainingSessionsRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            mapper ?? CreateMapper());
}
