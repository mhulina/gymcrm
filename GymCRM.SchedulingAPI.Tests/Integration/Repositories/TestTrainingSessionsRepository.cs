using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Models.Enums;

namespace GymCRM.SchedulingAPI.Tests.Integration.Repositories;

public class TestTrainingSessionsRepository : TestBase
{
    private readonly ITrainingSessionsRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestTrainingSessionsRepository()
    {
        _repository = new TrainingSessionsRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenTrainingSessionsExist_WhenFetchingAll_ThenAllAreReturned()
    {
        // Given
        await InsertDummySessions(3);

        // When
        var result = (await _repository.FetchAllAsync(CancellationToken.None)).ToList();

        // Then
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GivenTrainerId_WhenFetchingByCondition_ThenOnlyMatchingSessionsAreReturned()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var targetSession = CreateTrainingSession(trainerId);
        _repository.Add(targetSession);
        await InsertDummySessions(2);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(x => x.TrainerId == trainerId, CancellationToken.None)).ToList();

        // Then
        result.Should().ContainSingle(x => x.Id == targetSession.Id);
    }

    [Fact]
    public async Task GivenValidTrainingSession_WhenAdding_ThenTheSessionIsSaved()
    {
        // Given
        var session = CreateTrainingSession(Guid.CreateVersion7());

        // When
        _repository.Add(session);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var fetched = await _repository.FetchByConditionAsync(x => x.Id == session.Id, CancellationToken.None);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public async Task GivenMultipleTrainingSessions_WhenAddingRange_ThenAllAreSaved()
    {
        // When
        var result = await InsertDummySessions(4);

        // Then
        result.Should().BeTrue();
        var all = await _repository.FetchAllAsync(CancellationToken.None);
        all.Should().HaveCount(4);
    }

    [Fact]
    public async Task GivenExistingTrainingSession_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var session = CreateTrainingSession(Guid.CreateVersion7());
        _repository.Add(session);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        session.Status = (int)TrainingSessionStatus.Completed;
        _repository.Update(session);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = (await _repository.FetchByConditionAsync(x => x.Id == session.Id, CancellationToken.None)).First();
        updated.Status.Should().Be((int)TrainingSessionStatus.Completed);
    }

    [Fact]
    public async Task GivenExistingTrainingSession_WhenRemoving_ThenSessionIsDeleted()
    {
        // Given
        var session = CreateTrainingSession(Guid.CreateVersion7());
        _repository.Add(session);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        _repository.Remove(session);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var remaining = await _repository.FetchByConditionAsync(x => x.Id == session.Id, CancellationToken.None);
        remaining.Should().BeEmpty();
    }

    private async Task<bool> InsertDummySessions(int count)
    {
        var sessions = new List<TrainingSession>();

        for (var i = 0; i < count; i++)
        {
            sessions.Add(CreateTrainingSession(Guid.CreateVersion7()));
        }

        _repository.AddRange(sessions);
        return await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static TrainingSession CreateTrainingSession(Guid trainerId)
    {
        // StartTime/EndTime are mapped as "timestamp without time zone" (naive wall-clock,
        // see TrainingSessionsConfiguration.cs) - Npgsql rejects DateTimeKind.Utc values there.
        var startTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        return new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            TrainerId = trainerId,
            ClientId = Guid.CreateVersion7(),
            StartTime = startTime,
            EndTime = startTime.AddHours(1),
            Status = (int)TrainingSessionStatus.Booked,
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };
    }
}
