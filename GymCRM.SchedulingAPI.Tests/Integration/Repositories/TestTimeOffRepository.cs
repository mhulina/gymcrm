using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Tests.Integration.Repositories;

public class TestTimeOffRepository : TestBase
{
    private readonly ITimeOffRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestTimeOffRepository()
    {
        _repository = new TimeOffRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenTimeOffsExist_WhenFetchingAll_ThenAllAreReturned()
    {
        // Given
        await InsertDummyTimeOffs(3);

        // When
        var result = (await _repository.FetchAllAsync(CancellationToken.None)).ToList();

        // Then
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GivenTrainerId_WhenFetchingByCondition_ThenOnlyMatchingTimeOffsAreReturned()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var targetTimeOff = CreateTimeOff(trainerId);
        _repository.Add(targetTimeOff);
        await InsertDummyTimeOffs(2);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(x => x.TrainerId == trainerId, CancellationToken.None)).ToList();

        // Then
        result.Should().ContainSingle(x => x.Id == targetTimeOff.Id);
    }

    [Fact]
    public async Task GivenValidTimeOff_WhenAdding_ThenTheTimeOffIsSaved()
    {
        // Given
        var timeOff = CreateTimeOff(Guid.CreateVersion7());

        // When
        _repository.Add(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var fetched = await _repository.FetchByConditionAsync(x => x.Id == timeOff.Id, CancellationToken.None);
        fetched.Should().ContainSingle();
    }

    [Fact]
    public async Task GivenMultipleTimeOffs_WhenAddingRange_ThenAllAreSaved()
    {
        // When
        var result = await InsertDummyTimeOffs(4);

        // Then
        result.Should().BeTrue();
        var all = await _repository.FetchAllAsync(CancellationToken.None);
        all.Should().HaveCount(4);
    }

    [Fact]
    public async Task GivenExistingTimeOff_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var timeOff = CreateTimeOff(Guid.CreateVersion7());
        _repository.Add(timeOff);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        timeOff.Reason = "Updated reason";
        _repository.Update(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = (await _repository.FetchByConditionAsync(x => x.Id == timeOff.Id, CancellationToken.None)).First();
        updated.Reason.Should().Be("Updated reason");
    }

    [Fact]
    public async Task GivenExistingTimeOff_WhenRemoving_ThenTimeOffIsDeleted()
    {
        // Given
        var timeOff = CreateTimeOff(Guid.CreateVersion7());
        _repository.Add(timeOff);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        _repository.Remove(timeOff);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var remaining = await _repository.FetchByConditionAsync(x => x.Id == timeOff.Id, CancellationToken.None);
        remaining.Should().BeEmpty();
    }

    private async Task<bool> InsertDummyTimeOffs(int count)
    {
        var timeOffs = new List<TimeOff>();

        for (var i = 0; i < count; i++)
        {
            timeOffs.Add(CreateTimeOff(Guid.CreateVersion7()));
        }

        _repository.AddRange(timeOffs);
        return await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private static TimeOff CreateTimeOff(Guid trainerId) => new()
    {
        Id = Guid.CreateVersion7(),
        TrainerId = trainerId,
        Date = DateTime.UtcNow,
        Reason = "Personal",
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow
    };
}
