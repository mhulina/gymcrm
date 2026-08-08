using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Services.Implementation;
using GymCRM.SchedulingAPI.Tests.Unit.TestData;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Serilog;
using AvailabilityDto = GymCRM.SchedulingAPI.Models.DTOs.TrainerAvailability;
using InsertAvailability = GymCRM.SchedulingAPI.Models.DTOs.InsertAvailability;
using InsertDailyAvailability = GymCRM.SchedulingAPI.Models.DTOs.InsertDailyAvailability;
using InsertWorkingHours = GymCRM.SchedulingAPI.Models.DTOs.InsertWorkingHours;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestTrainerAvailabilitiesService
{
    private static readonly DateTime MondayDate = new(2024, 1, 1); // confirmed Monday

    [Fact]
    public async Task GivenAvailabilitiesExist_WhenGettingAllAvailabilities_ThenDailyAvailabilitiesArePopulated()
    {
        // Given
        var availabilities = CreateTrainerAvailability();
        var dailyAvailabilities = CreateTrainerDailyAvailability(availabilities);
        var workingHours = CreateTrainerWorkingHours(dailyAvailabilities);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availabilities.ToArray()).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailabilities.ToArray()).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours.ToArray()).Object);

        // When
        var result = (await service.GetAvailabilitiesAsync()).ToList();

        // Then
        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.DailyAvailabilities.Count == 7);
        result.SelectMany(a => a.DailyAvailabilities).Should().OnlyContain(d => d.WorkingHours.Count == 1);
    }

    [Fact]
    public async Task GivenNoAvailabilitiesExist_WhenGettingAllAvailabilities_ThenEmptyListIsReturnedWithoutExtraCalls()
    {
        // Given
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object);

        // When
        var result = await service.GetAvailabilitiesAsync();

        // Then
        result.Should().BeEmpty();
        dailyAvailabilitiesRepositoryMock.Verify(
            x => x.FetchByConditionAsync(It.IsAny<Expression<Func<TrainerDailyAvailability, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenTrainerId_WhenGettingAvailabilitiesForTrainerId_ThenOnlyThatTrainersAvailabilitiesAreReturned()
    {
        // Given
        var availabilities = CreateTrainerAvailability();
        var targetAvailability = availabilities[0];
        var dailyAvailabilities = CreateTrainerDailyAvailability(new List<TrainerAvailability> { targetAvailability });
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availabilities.ToArray()).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailabilities.ToArray()).Object);

        // When
        var result = (await service.GetAvailabilitiesForTrainerIdAsync(targetAvailability.TrainerId)).ToList();

        // Then
        result.Should().ContainSingle();
        result[0].TrainerId.Should().Be(targetAvailability.TrainerId);
        result[0].DailyAvailabilities.Should().HaveCount(7);
    }

    [Fact]
    public async Task GivenNoAvailabilities_WhenGettingTrainerIdsWithWorkingHours_ThenEmptyListIsReturned()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>().Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenDayOffWithNoWorkingHours_WhenGettingTrainerIdsWithWorkingHours_ThenTrainerIsExcluded()
    {
        // Given
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenDayOffWithStrayWorkingHours_WhenGettingTrainerIdsWithWorkingHours_ThenTrainerIsExcluded()
    {
        // Given - AddAvailabilityAsync doesn't itself enforce "day off implies zero working
        // hours" at creation time (only SetDayOffStatusAsync's toggle path cascade-deletes),
        // so a day-off day could still have a stray TrainerWorkingHours row. The trainer must
        // still be excluded - this pins the defensive !IsDayOff filter.
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var strayWorkingHours = new TrainerWorkingHours
        {
            Id = Guid.NewGuid(),
            DailyAvailabilityId = dailyAvailability.Id,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(strayWorkingHours).Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenWorkingDayWithNoWorkingHours_WhenGettingTrainerIdsWithWorkingHours_ThenTrainerIsExcluded()
    {
        // Given
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GivenWorkingDayWithWorkingHours_WhenGettingTrainerIdsWithWorkingHours_ThenTrainerIsIncluded()
    {
        // Given
        var (trainerId, availability, dailyAvailability, workingHours) = CreateOwnedWorkingHoursFixture();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours).Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().ContainSingle().Which.Should().Be(trainerId);
    }

    [Fact]
    public async Task GivenMultipleTrainersOneQualifying_WhenGettingTrainerIdsWithWorkingHours_ThenOnlyQualifyingTrainerIsReturned()
    {
        // Given
        var (qualifyingTrainerId, qualifyingAvailability, qualifyingDaily, qualifyingHours) = CreateOwnedWorkingHoursFixture();
        var nonQualifyingAvailability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var nonQualifyingDaily = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = nonQualifyingAvailability.Id,
            DayOfWeek = "Monday",
            IsDayOff = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(qualifyingAvailability, nonQualifyingAvailability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(qualifyingDaily, nonQualifyingDaily).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(qualifyingHours).Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().ContainSingle().Which.Should().Be(qualifyingTrainerId);
    }

    [Fact]
    public async Task GivenTrainerWithMultipleQualifyingWorkingHours_WhenGettingTrainerIdsWithWorkingHours_ThenTrainerIdAppearsOnce()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var mondayDaily = new TrainerDailyAvailability { Id = Guid.NewGuid(), AvailabilityId = availability.Id, DayOfWeek = "Monday", IsDayOff = false, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var tuesdayDaily = new TrainerDailyAvailability { Id = Guid.NewGuid(), AvailabilityId = availability.Id, DayOfWeek = "Tuesday", IsDayOff = false, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var mondayHours = new TrainerWorkingHours { Id = Guid.NewGuid(), DailyAvailabilityId = mondayDaily.Id, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var tuesdayHours = new TrainerWorkingHours { Id = Guid.NewGuid(), DailyAvailabilityId = tuesdayDaily.Id, StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(12, 0), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(mondayDaily, tuesdayDaily).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(mondayHours, tuesdayHours).Object);

        // When
        var result = await service.GetTrainerIdsWithWorkingHoursAsync();

        // Then
        result.Should().ContainSingle().Which.Should().Be(trainerId);
    }

    [Fact]
    public async Task GivenNullInsertAvailability_WhenAddingAvailability_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddAvailabilityAsync(null!, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenAddingAvailability_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();
        var insertAvailability = new InsertAvailability
        {
            TrainerId = Guid.NewGuid(),
            DailyAvailabilities = new List<InsertDailyAvailability>()
        };

        // When
        Func<Task> act = () => service.AddAvailabilityAsync(insertAvailability, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNoValidDailyAvailabilities_WhenAddingAvailability_ThenFalseIsReturnedWithoutSaving()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object);
        var trainerId = Guid.NewGuid();
        var insertAvailability = new InsertAvailability
        {
            TrainerId = trainerId,
            DailyAvailabilities = new List<InsertDailyAvailability>
            {
                new() { DayOfWeek = "NotARealDay", WorkingHours = new List<InsertWorkingHours>() }
            }
        };

        // When
        var result = await service.AddAvailabilityAsync(insertAvailability, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Add(It.IsAny<TrainerAvailability>()), Times.Never);
    }

    [Fact]
    public async Task GivenValidInsertAvailability_WhenAddingAvailability_ThenAvailabilityAndDailyDataAreSaved()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>();
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var trainerId = Guid.NewGuid();
        var insertAvailability = new InsertAvailability
        {
            TrainerId = trainerId,
            WorkingWeekends = true,
            DailyAvailabilities = new List<InsertDailyAvailability>
            {
                new()
                {
                    DayOfWeek = "Monday",
                    IsDayOff = false,
                    WorkingHours = new List<InsertWorkingHours> { new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) } }
                }
            }
        };

        // When
        var result = await service.AddAvailabilityAsync(insertAvailability, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Add(It.Is<TrainerAvailability>(a => a.TrainerId == insertAvailability.TrainerId)), Times.Once);
        dailyAvailabilitiesRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerDailyAvailability>>(d => d.Count() == 1)), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerWorkingHours>>(w => w.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task GivenAdminCaller_WhenAddingAvailabilityForAnotherTrainer_ThenSucceeds()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>().Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var insertAvailability = new InsertAvailability
        {
            TrainerId = Guid.NewGuid(),
            DailyAvailabilities = new List<InsertDailyAvailability>
            {
                new() { DayOfWeek = "Monday", WorkingHours = new List<InsertWorkingHours>() }
            }
        };

        // When - admin caller's own guid is unrelated to the trainer being modified
        var result = await service.AddAvailabilityAsync(insertAvailability, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Add(It.IsAny<TrainerAvailability>()), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenDeletingAvailability_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.DeleteAvailabilityAsync(Guid.Empty, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenAvailabilityNotFound_WhenDeletingAvailability_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object);

        // When
        var result = await service.DeleteAvailabilityAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenValidGuid_WhenDeletingAvailability_ThenAvailabilityIsRemoved()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var existingAvailability = new TrainerAvailability
        {
            Id = availabilityId,
            TrainerId = trainerId,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(existingAvailability);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When - the trainer deleting their own availability
        var result = await service.DeleteAvailabilityAsync(availabilityId, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Remove(It.Is<TrainerAvailability>(a => a.Id == availabilityId)), Times.Once);
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenDeletingAvailability_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var existingAvailability = new TrainerAvailability
        {
            Id = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(existingAvailability).Object);

        // When
        Func<Task> act = () => service.DeleteAvailabilityAsync(existingAvailability.Id, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNullTrainerAvailability_WhenUpdatingAvailability_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.UpdateAvailabilityAsync(null!, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidTrainerAvailability_WhenUpdatingAvailability_ThenAvailabilityIsUpdated()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availabilityId = Guid.NewGuid();
        var existingAvailability = new TrainerAvailability
        {
            Id = availabilityId,
            TrainerId = trainerId,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(existingAvailability);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            mapper: CreateMapper());
        var dto = new AvailabilityDto
        {
            Id = availabilityId,
            TrainerId = trainerId,
            WorkingWeekends = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        // When - the trainer updating their own availability
        var result = await service.UpdateAvailabilityAsync(dto, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Update(It.Is<TrainerAvailability>(a => a.Id == dto.Id && a.TrainerId == dto.TrainerId)), Times.Once);
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenUpdatingAvailability_ThenAccessDeniedExceptionIsThrown()
    {
        // Given - authorization must be checked against the EXISTING row's TrainerId, not the
        // client-supplied dto.TrainerId, otherwise a caller could hijack someone else's row by
        // submitting their own guid in the body. This dto deliberately sets TrainerId to the
        // caller's own guid to prove that doesn't bypass the check.
        var existingAvailability = new TrainerAvailability
        {
            Id = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var callerAccountGuid = Guid.NewGuid();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(existingAvailability).Object,
            mapper: CreateMapper());
        var dto = new AvailabilityDto
        {
            Id = existingAvailability.Id,
            TrainerId = callerAccountGuid,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        // When
        Func<Task> act = () => service.UpdateAvailabilityAsync(dto, callerAccountGuid, callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenEmptyTrainerId_WhenAddingWorkingHoursToDailyAvailability_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddWorkingHoursToDailyAvailability(
            Guid.Empty, "Monday", new List<InsertWorkingHours>(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidDayName_WhenAddingWorkingHoursToDailyAvailability_ThenInvalidOperationExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddWorkingHoursToDailyAvailability(
            Guid.NewGuid(), "NotARealDay", new List<InsertWorkingHours>(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenAddingWorkingHoursToDailyAvailability_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddWorkingHoursToDailyAvailability(
            Guid.NewGuid(), "Monday", new List<InsertWorkingHours>(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenNoNewWorkingHours_WhenAddingWorkingHoursToDailyAvailability_ThenTrueIsReturnedWithoutChanges()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object);

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(
            trainerId, "Monday", new List<InsertWorkingHours>(), trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(
            x => x.FetchByConditionAsync(It.IsAny<Expression<Func<TrainerAvailability, bool>>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenTrainerHasNoAvailability_WhenAddingWorkingHoursToDailyAvailability_ThenFalseIsReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object);
        var newWorkingHours = new List<InsertWorkingHours> { new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) } };

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(trainerId, "Monday", newWorkingHours, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenExistingDailyAvailabilityForDay_WhenAddingWorkingHoursToDailyAvailability_ThenWorkingHoursAreAddedToIt()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability);
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var newWorkingHours = new List<InsertWorkingHours> { new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) } };

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(trainerId, "Monday", newWorkingHours, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Add(It.IsAny<TrainerDailyAvailability>()), Times.Never);
        workingHoursRepositoryMock.Verify(
            x => x.AddRange(It.Is<IEnumerable<TrainerWorkingHours>>(w => w.All(wh => wh.DailyAvailabilityId == dailyAvailability.Id))),
            Times.Once);
    }

    [Fact]
    public async Task GivenNoDailyAvailabilityForDayYet_WhenAddingWorkingHoursToDailyAvailability_ThenDailyAvailabilityIsCreatedThenWorkingHoursAdded()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>();
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var newWorkingHours = new List<InsertWorkingHours> { new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) } };

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(trainerId, "Tuesday", newWorkingHours, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Add(It.Is<TrainerDailyAvailability>(d => d.AvailabilityId == availability.Id && d.DayOfWeek == "Tuesday" && !d.IsDayOff)), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerWorkingHours>>(w => w.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyId_WhenUpdatingWorkingHours_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.UpdateWorkingHoursAsync(
            Guid.Empty, new InsertWorkingHours(), Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenNullUpdatedWorkingHours_WhenUpdatingWorkingHours_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.UpdateWorkingHoursAsync(
            Guid.NewGuid(), null!, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenWorkingHoursNotFound_WhenUpdatingWorkingHours_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService(
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object);

        // When
        var result = await service.UpdateWorkingHoursAsync(
            Guid.NewGuid(), new InsertWorkingHours { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(9, 0) }, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenSelfCaller_WhenUpdatingWorkingHours_ThenStartAndEndTimeAreUpdated()
    {
        // Given
        var (trainerId, _, dailyAvailability, workingHours) = CreateOwnedWorkingHoursFixture();
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(
                new TrainerAvailability { Id = dailyAvailability.AvailabilityId, TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow }).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var updated = new InsertWorkingHours { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0) };

        // When
        var result = await service.UpdateWorkingHoursAsync(workingHours.Id, updated, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        workingHours.StartTime.Should().Be(new TimeOnly(9, 0));
        workingHours.EndTime.Should().Be(new TimeOnly(18, 0));
        workingHoursRepositoryMock.Verify(x => x.Update(workingHours), Times.Once);
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenUpdatingWorkingHours_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var (_, _, dailyAvailability, workingHours) = CreateOwnedWorkingHoursFixture();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(
                new TrainerAvailability { Id = dailyAvailability.AvailabilityId, TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow }).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours).Object);
        var updated = new InsertWorkingHours { StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(18, 0) };

        // When
        Func<Task> act = () => service.UpdateWorkingHoursAsync(workingHours.Id, updated, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenEmptyId_WhenDeletingWorkingHours_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.DeleteWorkingHoursAsync(Guid.Empty, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenWorkingHoursNotFound_WhenDeletingWorkingHours_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService(
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>().Object);

        // When
        var result = await service.DeleteWorkingHoursAsync(Guid.NewGuid(), Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenAdminCaller_WhenDeletingAnotherTrainersWorkingHours_ThenWorkingHoursAreRemoved()
    {
        // Given
        var (_, _, dailyAvailability, workingHours) = CreateOwnedWorkingHoursFixture();
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(
                new TrainerAvailability { Id = dailyAvailability.AvailabilityId, TrainerId = Guid.NewGuid(), DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow }).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When - admin caller's own guid is unrelated to the trainer who owns these hours
        var result = await service.DeleteWorkingHoursAsync(workingHours.Id, Guid.NewGuid(), callerIsAdmin: true);

        // Then
        result.Should().BeTrue();
        workingHoursRepositoryMock.Verify(x => x.Remove(workingHours), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyTrainerId_WhenSettingDayOffStatus_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.SetDayOffStatusAsync(Guid.Empty, "Monday", true, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidDayName_WhenSettingDayOffStatus_ThenInvalidOperationExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.SetDayOffStatusAsync(Guid.NewGuid(), "NotARealDay", true, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenNonOwningNonAdminCaller_WhenSettingDayOffStatus_ThenAccessDeniedExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.SetDayOffStatusAsync(Guid.NewGuid(), "Monday", true, Guid.NewGuid(), callerIsAdmin: false);

        // Then
        await act.Should().ThrowAsync<TrainerAvailabilityAccessDeniedException>();
    }

    [Fact]
    public async Task GivenTrainerHasNoAvailability_WhenSettingDayOffStatus_ThenFalseIsReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object);

        // When
        var result = await service.SetDayOffStatusAsync(trainerId, "Monday", true, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenDayHasNoRowYetAndNotBeingSetToDayOff_WhenSettingDayOffStatus_ThenNoOpSuccessIsReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object);

        // When
        var result = await service.SetDayOffStatusAsync(trainerId, "Monday", false, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Add(It.IsAny<TrainerDailyAvailability>()), Times.Never);
    }

    [Fact]
    public async Task GivenDayHasNoRowYetAndBeingSetToDayOff_WhenSettingDayOffStatus_ThenDayOffRowIsCreated()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.SetDayOffStatusAsync(trainerId, "Monday", true, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Add(It.Is<TrainerDailyAvailability>(d => d.AvailabilityId == availability.Id && d.DayOfWeek == "Monday" && d.IsDayOff)), Times.Once);
    }

    [Fact]
    public async Task GivenExistingWorkingDayBeingToggledToDayOff_WhenSettingDayOffStatus_ThenWorkingHoursAreRemoved()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var workingHours = new TrainerWorkingHours
        {
            Id = Guid.NewGuid(),
            DailyAvailabilityId = dailyAvailability.Id,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability);
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours);
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.SetDayOffStatusAsync(trainerId, "Monday", true, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailability.IsDayOff.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Update(dailyAvailability), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.Remove(workingHours), Times.Once);
    }

    [Fact]
    public async Task GivenExistingDayOffBeingToggledToWorking_WhenSettingDayOffStatus_ThenDayIsUpdatedWithoutTouchingWorkingHours()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var dailyAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability);
        var workingHoursRepositoryMock = CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: dailyAvailabilitiesRepositoryMock.Object,
            trainerWorkingHoursRepository: workingHoursRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);

        // When
        var result = await service.SetDayOffStatusAsync(trainerId, "Monday", false, trainerId, callerIsAdmin: false);

        // Then
        result.Should().BeTrue();
        dailyAvailability.IsDayOff.Should().BeFalse();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Update(dailyAvailability), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.Remove(It.IsAny<TrainerWorkingHours>()), Times.Never);
    }

    [Fact]
    public async Task GivenEmptyTrainerId_WhenCheckingIsTrainerWorkingOnDate_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.IsTrainerWorkingOnDateAsync(Guid.Empty, MondayDate, MondayDate.AddHours(1));

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [MemberData(
        nameof(TrainerAvailabilitiesServiceTestData.InvalidStartOrEndTimesForAvailabilityCheck),
        MemberType = typeof(TrainerAvailabilitiesServiceTestData))]
    public async Task GivenInvalidStartOrEndTime_WhenCheckingIsTrainerWorkingOnDate_ThenArgumentExceptionIsThrown(DateTime startTime, DateTime endTime)
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.IsTrainerWorkingOnDateAsync(Guid.NewGuid(), startTime, endTime);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenTrainerHasNoAvailabilities_WhenCheckingIsTrainerWorkingOnDate_ThenFalseIsReturned()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object);

        // When
        var result = await service.IsTrainerWorkingOnDateAsync(Guid.NewGuid(), MondayDate.AddHours(9), MondayDate.AddHours(10));

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenTrainerHasDayOff_WhenCheckingIsTrainerWorkingOnDate_ThenFalseIsReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object);

        // When
        var result = await service.IsTrainerWorkingOnDateAsync(trainerId, MondayDate.AddHours(9), MondayDate.AddHours(10));

        // Then
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GivenRequestedTimeWithinWorkingHours_WhenCheckingIsTrainerWorkingOnDate_ThenTrueIsReturned()
    {
        // Given
        var service = CreateWorkingTrainerService(out var trainerId);

        // When
        var result = await service.IsTrainerWorkingOnDateAsync(trainerId, MondayDate.AddHours(9), MondayDate.AddHours(10));

        // Then
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GivenRequestedTimeOutsideWorkingHours_WhenCheckingIsTrainerWorkingOnDate_ThenFalseIsReturned()
    {
        // Given
        var service = CreateWorkingTrainerService(out var trainerId);

        // When
        var result = await service.IsTrainerWorkingOnDateAsync(trainerId, MondayDate.AddHours(9), MondayDate.AddHours(18));

        // Then
        result.Should().BeFalse();
    }

    // Builds a trainer with one existing daily availability + one existing working-hours range,
    // for the UpdateWorkingHoursAsync/DeleteWorkingHoursAsync ownership-resolution tests, which
    // only ever receive a bare TrainerWorkingHours.Id and must resolve the owning trainer via
    // the DailyAvailability -> Availability chain.
    private static (Guid trainerId, TrainerAvailability availability, TrainerDailyAvailability dailyAvailability, TrainerWorkingHours workingHours) CreateOwnedWorkingHoursFixture()
    {
        var trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var workingHours = new TrainerWorkingHours
        {
            Id = Guid.NewGuid(),
            DailyAvailabilityId = dailyAvailability.Id,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        return (trainerId, availability, dailyAvailability, workingHours);
    }

    // Wires up a trainer working Mondays 08:00-17:00, for the IsTrainerWorkingOnDateAsync
    // boundary tests above.
    private static TrainerAvailabilitiesService CreateWorkingTrainerService(out Guid trainerId)
    {
        trainerId = Guid.NewGuid();
        var availability = new TrainerAvailability { Id = Guid.NewGuid(), TrainerId = trainerId, DateCreatedUtc = DateTime.UtcNow, DateModifiedUtc = DateTime.UtcNow };
        var dailyAvailability = new TrainerDailyAvailability
        {
            Id = Guid.NewGuid(),
            AvailabilityId = availability.Id,
            DayOfWeek = "Monday",
            IsDayOff = false,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };
        var workingHours = new TrainerWorkingHours
        {
            Id = Guid.NewGuid(),
            DailyAvailabilityId = dailyAvailability.Id,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(17, 0),
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        return CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>(availability).Object,
            trainerDailyAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerDailyAvailabilitiesRepository, TrainerDailyAvailability>(dailyAvailability).Object,
            trainerWorkingHoursRepository: CreateGenericRepositoryMock<ITrainerWorkingHoursRepository, TrainerWorkingHours>(workingHours).Object);
    }

    private static List<TrainerAvailability> CreateTrainerAvailability()
    {
        var trainerAvailability = new List<TrainerAvailability>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                TrainerId = Guid.Parse("019b9571-cd4d-7381-814a-21cdccb05aec"),
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow,
                WorkingWeekends = false
            },
            new()
            {
                Id = Guid.CreateVersion7(),
                TrainerId = Guid.Parse("019b957b-64f6-7f50-9d7e-855897552f5b"),
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow,
                WorkingWeekends = true
            }
        };

        return trainerAvailability;
    }

    private static List<TrainerDailyAvailability> CreateTrainerDailyAvailability(
        List<TrainerAvailability> trainerAvailabilities)
    {
        var trainerDailyAvailabilities = new List<TrainerDailyAvailability>();

        foreach (var trainerAvailability in trainerAvailabilities)
        {
            for (var j = 0; j < 7; j++)
            {
                var trainerDailyAvailability = new TrainerDailyAvailability
                {
                    Id = Guid.CreateVersion7(),
                    AvailabilityId = trainerAvailability.Id,
                    DateCreatedUtc = DateTime.UtcNow,
                    DateModifiedUtc = DateTime.UtcNow,
                    DayOfWeek = ((DayOfWeek)j).ToString(),
                    IsDayOff = j == 3
                };

                trainerDailyAvailabilities.Add(trainerDailyAvailability);
            }
        }

        return trainerDailyAvailabilities;
    }

    private static List<TrainerWorkingHours> CreateTrainerWorkingHours(
        List<TrainerDailyAvailability> trainerDailyAvailabilities)
    {
        var trainersWorkingHours = new List<TrainerWorkingHours>();

        foreach (var trainerDailyAvailability in trainerDailyAvailabilities)
        {
            var trainerWorkingHours = new TrainerWorkingHours
            {
                Id = Guid.CreateVersion7(),
                DailyAvailabilityId = trainerDailyAvailability.Id,
                DateCreatedUtc = DateTime.UtcNow,
                DateModifiedUtc = DateTime.UtcNow,
                StartTime = new TimeOnly(7, 30),
                EndTime = new TimeOnly(16, 30)
            };

            trainersWorkingHours.Add(trainerWorkingHours);
        }

        return trainersWorkingHours;
    }

    // Backs FetchAllAsync/FetchByConditionAsync with an in-memory list and compiles/applies the
    // predicate expression against it - works for any IGenericRepository<TEntity> implementation.
    private static Mock<TRepo> CreateGenericRepositoryMock<TRepo, TEntity>(params TEntity[] entities)
        where TRepo : class, IGenericRepository<TEntity>
        where TEntity : class
    {
        var backingList = entities.ToList();
        var repositoryMock = new Mock<TRepo>();
        repositoryMock.Setup(x => x.FetchAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(backingList);
        repositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<TEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> expression, CancellationToken _) =>
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

    private static TrainerAvailabilitiesService CreateTrainerAvailabilitiesService(
        ITrainerWorkingHoursRepository? trainerWorkingHoursRepository = null,
        ITrainerAvailabilitiesRepository? trainerAvailabilitiesRepository = null,
        ITrainerDailyAvailabilitiesRepository? trainerDailyAvailabilitiesRepository = null,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null,
        ILogger? logger = null) =>
        new(
            trainerWorkingHoursRepository ?? Mock.Of<ITrainerWorkingHoursRepository>(),
            trainerAvailabilitiesRepository ?? Mock.Of<ITrainerAvailabilitiesRepository>(),
            trainerDailyAvailabilitiesRepository ?? Mock.Of<ITrainerDailyAvailabilitiesRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            mapper ?? CreateMapper(),
            logger ?? Mock.Of<ILogger>());
}
