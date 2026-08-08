using System.Linq.Expressions;
using AutoMapper;
using FluentAssertions;
using GymCRM.SchedulingAPI.Infrastructure.Interface;
using GymCRM.SchedulingAPI.Models.Entities;
using GymCRM.SchedulingAPI.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using InsertTimeOff = GymCRM.SchedulingAPI.Models.DTOs.InsertTimeOff;
using TimeOffDto = GymCRM.SchedulingAPI.Models.DTOs.TimeOff;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestTimeOffService
{
    [Fact]
    public async Task GivenTimeOffsExist_WhenGettingAll_ThenMappedTimeOffsAreReturned()
    {
        // Given
        var timeOff = CreateTimeOff();
        var service = CreateTimeOffService(repository: CreateRepositoryMock(timeOff).Object);

        // When
        var result = await service.GetAllAsync();

        // Then
        result.Should().ContainSingle(t => t.Id == timeOff.Id);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenGettingAllForTrainerId_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.GetAllForTrainerIdAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidTrainerId_WhenGettingAllForTrainerId_ThenOnlyThatTrainersTimeOffsAreReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var ownTimeOff = CreateTimeOff(trainerId: trainerId);
        var otherTimeOff = CreateTimeOff();
        var service = CreateTimeOffService(repository: CreateRepositoryMock(ownTimeOff, otherTimeOff).Object);

        // When
        var result = await service.GetAllForTrainerIdAsync(trainerId);

        // Then
        result.Should().ContainSingle(t => t.Id == ownTimeOff.Id);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenGettingAllForTrainerIdInMonth_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.GetAllForTrainerIdInMonthAsync(Guid.Empty, 1);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidTrainerIdAndMonth_WhenGettingAllForTrainerIdInMonth_ThenOnlyMatchingTimeOffsAreReturned()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var matchingTimeOff = CreateTimeOff(trainerId: trainerId, date: new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        var otherMonthTimeOff = CreateTimeOff(trainerId: trainerId, date: new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc));
        var service = CreateTimeOffService(repository: CreateRepositoryMock(matchingTimeOff, otherMonthTimeOff).Object);

        // When
        var result = await service.GetAllForTrainerIdInMonthAsync(trainerId, 3);

        // Then
        result.Should().ContainSingle(t => t.Id == matchingTimeOff.Id);
    }

    [Fact]
    public async Task GivenEndDateBeforeStartDate_WhenGettingAllForDatePeriod_ThenInvalidOperationExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.GetAllForDatePeriodAsync(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenMinValueStartDate_WhenGettingAllForDatePeriod_ThenInvalidOperationExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.GetAllForDatePeriodAsync(DateTime.MinValue, DateTime.UtcNow);

        // Then
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GivenMaxValueEndDate_WhenGettingAllForDatePeriod_ThenNoExceptionIsThrown()
    {
        // Given - the XML doc on ITimeOffService.GetAllForDatePeriodAsync claims either date
        // being MaxValue throws, but the implementation's guard clause only checks
        // startDate == MinValue/MaxValue and endDate == MinValue - endDate == MaxValue slips
        // through uncaught. Pinning this actual (documented-but-not-implemented) behavior.
        var service = CreateTimeOffService(repository: CreateRepositoryMock().Object);

        // When
        Func<Task> act = () => service.GetAllForDatePeriodAsync(DateTime.UtcNow, DateTime.MaxValue);

        // Then
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GivenTimeOffsInRange_WhenGettingAllForDatePeriod_ThenTheyAreGroupedByDate()
    {
        // Given
        var date = new DateTime(2024, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var firstOnDate = CreateTimeOff(date: date);
        var secondOnDate = CreateTimeOff(date: date);
        var outsideRange = CreateTimeOff(date: date.AddMonths(2));
        var service = CreateTimeOffService(repository: CreateRepositoryMock(firstOnDate, secondOnDate, outsideRange).Object);

        // When
        var result = await service.GetAllForDatePeriodAsync(date.AddDays(-1), date.AddDays(1));

        // Then
        result.Should().ContainKey(date);
        result[date].Should().HaveCount(2);
    }

    [Fact]
    public async Task GivenNullInsertTimeOff_WhenAddingTimeOff_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.AddTimeOffAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidInsertTimeOff_WhenAddingTimeOff_ThenTimeOffIsAdded()
    {
        // Given
        var repositoryMock = new Mock<ITimeOffRepository>();
        var service = CreateTimeOffService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var insertTimeOff = new InsertTimeOff { TrainerId = Guid.NewGuid(), Date = DateTime.UtcNow, Reason = "Vacation" };

        // When
        var result = await service.AddTimeOffAsync(insertTimeOff);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Add(It.Is<TimeOff>(t => t.TrainerId == insertTimeOff.TrainerId && t.Reason == "Vacation")), Times.Once);
    }

    [Fact]
    public async Task GivenEmptyGuid_WhenDeletingTimeOff_ThenArgumentExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.DeleteTimeOffAsync(Guid.Empty);

        // Then
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GivenValidGuid_WhenDeletingTimeOff_ThenTimeOffIsRemoved()
    {
        // Given
        var repositoryMock = new Mock<ITimeOffRepository>();
        var service = CreateTimeOffService(repository: repositoryMock.Object, unitOfWork: CreateUnitOfWorkMock(true).Object);
        var timeOffId = Guid.NewGuid();

        // When
        var result = await service.DeleteTimeOffAsync(timeOffId);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Remove(It.Is<TimeOff>(t => t.Id == timeOffId)), Times.Once);
    }

    [Fact]
    public async Task GivenNullUpdatedTimeOff_WhenUpdatingTimeOff_ThenArgumentNullExceptionIsThrown()
    {
        // Given
        var service = CreateTimeOffService();

        // When
        Func<Task> act = () => service.UpdateTimeOffAsync(null!);

        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GivenValidUpdatedTimeOff_WhenUpdatingTimeOff_ThenTimeOffIsUpdated()
    {
        // Given
        var repositoryMock = new Mock<ITimeOffRepository>();
        var service = CreateTimeOffService(
            repository: repositoryMock.Object,
            unitOfWork: CreateUnitOfWorkMock(true).Object,
            mapper: CreateMapper());
        var dto = new TimeOffDto { Id = Guid.NewGuid(), TrainerId = Guid.NewGuid(), Date = DateTime.UtcNow, Reason = "Updated reason" };

        // When
        var result = await service.UpdateTimeOffAsync(dto);

        // Then
        result.Should().BeTrue();
        repositoryMock.Verify(x => x.Update(It.Is<TimeOff>(t => t.Id == dto.Id && t.Reason == "Updated reason")), Times.Once);
    }

    private static TimeOff CreateTimeOff(Guid? trainerId = null, DateTime? date = null) => new()
    {
        Id = Guid.NewGuid(),
        TrainerId = trainerId ?? Guid.NewGuid(),
        Date = date ?? DateTime.UtcNow,
        Reason = "Personal",
        DateCreated = DateTime.UtcNow,
        DateModified = DateTime.UtcNow
    };

    // Backs FetchAllAsync/FetchByConditionAsync with an in-memory list and compiles/applies the
    // predicate expression against it.
    private static Mock<ITimeOffRepository> CreateRepositoryMock(params TimeOff[] timeOffs)
    {
        var backingList = timeOffs.ToList();
        var repositoryMock = new Mock<ITimeOffRepository>();
        repositoryMock.Setup(x => x.FetchAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(backingList);
        repositoryMock
            .Setup(x => x.FetchByConditionAsync(It.IsAny<Expression<Func<TimeOff, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<TimeOff, bool>> expression, CancellationToken _) =>
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

    private static TimeOffService CreateTimeOffService(
        ITimeOffRepository? repository = null,
        IUnitOfWork? unitOfWork = null,
        IMapper? mapper = null) =>
        new(
            repository ?? Mock.Of<ITimeOffRepository>(),
            unitOfWork ?? Mock.Of<IUnitOfWork>(),
            mapper ?? CreateMapper());
}
