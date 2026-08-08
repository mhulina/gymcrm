using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
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
    public async Task GivenNullInsertAvailability_WhenAddingAvailability_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddAvailabilityAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenNoValidDailyAvailabilities_WhenAddingAvailability_ThenFalseIsReturnedWithoutSaving()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object);
        var insertAvailability = new InsertAvailability
        {
            TrainerId = Guid.NewGuid(),
            DailyAvailabilities = new List<InsertDailyAvailability>
            {
                new() { DayOfWeek = "NotARealDay", WorkingHours = new List<InsertWorkingHours>() }
            }
        };

        // When
        var result = await service.AddAvailabilityAsync(insertAvailability);

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
        var insertAvailability = new InsertAvailability
        {
            TrainerId = Guid.NewGuid(),
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
        var result = await service.AddAvailabilityAsync(insertAvailability);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Add(It.Is<TrainerAvailability>(a => a.TrainerId == insertAvailability.TrainerId)), Times.Once);
        dailyAvailabilitiesRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerDailyAvailability>>(d => d.Count() == 1)), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerWorkingHours>>(w => w.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenDeletingAvailability_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.DeleteAvailabilityAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidGuid_WhenDeletingAvailability_ThenAvailabilityIsRemoved()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object);
        var availabilityId = Guid.NewGuid();

        // When
        var result = await service.DeleteAvailabilityAsync(availabilityId);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Remove(It.Is<TrainerAvailability>(a => a.Id == availabilityId)), Times.Once);
    }

    [Fact]
    public async Task GivenNullTrainerAvailability_WhenUpdatingAvailability_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.UpdateAvailabilityAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidTrainerAvailability_WhenUpdatingAvailability_ThenAvailabilityIsUpdated()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            mapper: CreateMapper());
        var dto = new AvailabilityDto
        {
            Id = Guid.NewGuid(),
            TrainerId = Guid.NewGuid(),
            WorkingWeekends = true,
            DateCreatedUtc = DateTime.UtcNow,
            DateModifiedUtc = DateTime.UtcNow
        };

        // When
        var result = await service.UpdateAvailabilityAsync(dto);

        // Then
        result.Should().BeTrue();
        trainerAvailabilitiesRepositoryMock.Verify(x => x.Update(It.Is<TrainerAvailability>(a => a.Id == dto.Id && a.TrainerId == dto.TrainerId)), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyTrainerId_WhenAddingWorkingHoursToDailyAvailability_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddWorkingHoursToDailyAvailability(Guid.Empty, "Monday", new List<InsertWorkingHours>());

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenInvalidDayName_WhenAddingWorkingHoursToDailyAvailability_ThenInvalidOperationExceptionIsThrown()
    {
        // Given
        var service = CreateTrainerAvailabilitiesService();

        // When
        Func<Task> act = () => service.AddWorkingHoursToDailyAvailability(Guid.NewGuid(), "NotARealDay", new List<InsertWorkingHours>());

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenNoNewWorkingHours_WhenAddingWorkingHoursToDailyAvailability_ThenTrueIsReturnedWithoutChanges()
    {
        // Given
        var trainerAvailabilitiesRepositoryMock = CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>();
        var service = CreateTrainerAvailabilitiesService(trainerAvailabilitiesRepository: trainerAvailabilitiesRepositoryMock.Object);

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(Guid.NewGuid(), "Monday", new List<InsertWorkingHours>());

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
        var service = CreateTrainerAvailabilitiesService(
            trainerAvailabilitiesRepository: CreateGenericRepositoryMock<ITrainerAvailabilitiesRepository, TrainerAvailability>().Object);
        var newWorkingHours = new List<InsertWorkingHours> { new() { StartTime = new TimeOnly(8, 0), EndTime = new TimeOnly(17, 0) } };

        // When
        var result = await service.AddWorkingHoursToDailyAvailability(Guid.NewGuid(), "Monday", newWorkingHours);

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
        var result = await service.AddWorkingHoursToDailyAvailability(trainerId, "Monday", newWorkingHours);

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
        var result = await service.AddWorkingHoursToDailyAvailability(trainerId, "Tuesday", newWorkingHours);

        // Then
        result.Should().BeTrue();
        dailyAvailabilitiesRepositoryMock.Verify(x => x.Add(It.Is<TrainerDailyAvailability>(d => d.AvailabilityId == availability.Id && d.DayOfWeek == "Tuesday")), Times.Once);
        workingHoursRepositoryMock.Verify(x => x.AddRange(It.Is<IEnumerable<TrainerWorkingHours>>(w => w.Count() == 1)), Times.Once);
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
