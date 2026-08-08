using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Implementation;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Tests.Integration.Repositories;

public class TestTrainerAvailabilitiesRepository : TestBase
{
    private readonly ITrainerAvailabilitiesRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public TestTrainerAvailabilitiesRepository()
    {
        _repository = new TrainerAvailabilitiesRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
    }

    [Fact]
    public async Task GivenValidTrainerAvailability_WhenInsertingAvailability_ThenTheAvailabilityIsSaved()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var trainerAvailability = new TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = trainerId,
            WorkingWeekends = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        
        // When
        _repository.Add(trainerAvailability);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);
        
        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GivenValidData_WhenGettingAllTrainerAvailabilities_ThenAllTrainerAvailabilitiesAreReturned()
    {
        // Given
        var dummyDataInserted = await InsertDummyDataIntoRepository();
        dummyDataInserted.Should().BeTrue();
        
        // When
        var result = (await _repository.FetchAllAsync(CancellationToken.None)).ToList();
        
        // Then
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(5);
    }
    
    [Fact]
    public async Task GivenTrainerId_WhenFetchingByCondition_ThenOnlyMatchingAvailabilitiesAreReturned()
    {
        // Given
        await InsertDummyDataIntoRepository();
        var targetTrainerId = Guid.CreateVersion7();
        var targetAvailability = new TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = targetTrainerId,
            WorkingWeekends = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        _repository.Add(targetAvailability);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        var result = (await _repository.FetchByConditionAsync(x => x.TrainerId == targetTrainerId, CancellationToken.None)).ToList();

        // Then
        result.Should().ContainSingle(x => x.Id == targetAvailability.Id);
    }

    [Fact]
    public async Task GivenExistingAvailability_WhenRemoving_ThenAvailabilityIsDeleted()
    {
        // Given
        var trainerAvailability = new TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            WorkingWeekends = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        _repository.Add(trainerAvailability);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        _repository.Remove(trainerAvailability);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var remaining = await _repository.FetchByConditionAsync(x => x.Id == trainerAvailability.Id, CancellationToken.None);
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenExistingAvailability_WhenUpdating_ThenChangesArePersisted()
    {
        // Given
        var trainerAvailability = new TrainerAvailability
        {
            Id = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            WorkingWeekends = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        _repository.Add(trainerAvailability);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // When
        trainerAvailability.WorkingWeekends = true;
        _repository.Update(trainerAvailability);
        var result = await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Then
        result.Should().BeTrue();
        var updated = (await _repository.FetchByConditionAsync(x => x.Id == trainerAvailability.Id, CancellationToken.None)).First();
        updated.WorkingWeekends.Should().BeTrue();
    }

    private async Task<bool> InsertDummyDataIntoRepository()
    {
        var trainerAvailabilities = new List<TrainerAvailability>();

        for (var i = 0; i < 5; i++)
        {
            var trainerAvailability = new TrainerAvailability
            {
                Id = Guid.CreateVersion7(),
                TrainerId = Guid.CreateVersion7(),
                WorkingWeekends = false,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow
            };
            
            trainerAvailabilities.Add(trainerAvailability);
        }
        
        _repository.AddRange(trainerAvailabilities);
        return await _unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}