using FluentAssertions;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Services.Implementation;
using GymCRM.SchedulingAPI.Services.Interface;
using Moq;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestCalendarService
{
    [Fact]
    public async Task GivenDataAcrossMultipleMonths_WhenGettingCalendarForMonth_ThenOnlyMatchingMonthDataIsIncluded()
    {
        // Given
        var trainerId = Guid.NewGuid();
        var timeOffInMonth = new TimeOff { TrainerId = trainerId, Date = new DateTime(2024, 6, 10) };
        var timeOffOutsideMonth = new TimeOff { TrainerId = trainerId, Date = new DateTime(2024, 7, 10) };
        var sessionInMonth = new TrainingSession
        {
            TrainerId = trainerId,
            StartTime = new DateTime(2024, 6, 5),
            EndTime = new DateTime(2024, 6, 5).AddHours(1)
        };
        var sessionOutsideMonth = new TrainingSession
        {
            TrainerId = trainerId,
            StartTime = new DateTime(2024, 7, 5),
            EndTime = new DateTime(2024, 7, 5).AddHours(1)
        };
        var timeOffServiceMock = new Mock<ITimeOffService>();
        timeOffServiceMock
            .Setup(x => x.GetAllForTrainerIdAsync(trainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeOff> { timeOffInMonth, timeOffOutsideMonth });
        var trainingSessionsServiceMock = new Mock<ITrainingSessionsService>();
        trainingSessionsServiceMock
            .Setup(x => x.GetTrainingSessionsForTrainerIdAsync(trainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingSession> { sessionInMonth, sessionOutsideMonth });
        var service = CreateCalendarService(
            timeOffService: timeOffServiceMock.Object,
            trainingSessionsService: trainingSessionsServiceMock.Object);

        // When
        var result = await service.GetGymTrainerCalendarForMonthAsync(trainerId, 6, 2024);

        // Then
        result.Month.Should().Be(6);
        result.Year.Should().Be(2024);
        result.TrainerId.Should().Be(trainerId);
        result.TimeOffs.Should().ContainSingle().Which.Should().Be(timeOffInMonth);
        result.TrainingSessions.Should().ContainSingle().Which.Should().Be(sessionInMonth);
    }

    [Fact]
    public async Task GivenAvailabilitiesExistForTrainer_WhenGettingCalendarForMonth_ThenAvailabilitiesAreNotPopulatedOnResult()
    {
        // Given - GetGymTrainerCalendarForMonthAsync fetches the trainer's availabilities into a
        // local variable but never assigns them onto GymTrainerCalendarDto.Availabilities, so
        // that field is always null on the returned DTO despite the fetch happening. Pinning
        // this actual (likely unintentional) behavior - ICalendarService also has no controller
        // consumer anywhere in the app today, so this has never surfaced.
        var trainerId = Guid.NewGuid();
        var trainerAvailabilitiesServiceMock = new Mock<ITrainerAvailabilitiesService>();
        trainerAvailabilitiesServiceMock
            .Setup(x => x.GetAvailabilitiesForTrainerIdAsync(trainerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainerAvailability> { new() { TrainerId = trainerId } });
        var service = CreateCalendarService(trainerAvailabilitiesService: trainerAvailabilitiesServiceMock.Object);

        // When
        var result = await service.GetGymTrainerCalendarForMonthAsync(trainerId, 1, 2024);

        // Then
        result.Availabilities.Should().BeNull();
    }

    private static ITrainerAvailabilitiesService DefaultTrainerAvailabilitiesService()
    {
        var mock = new Mock<ITrainerAvailabilitiesService>();
        mock.Setup(x => x.GetAvailabilitiesForTrainerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainerAvailability>());

        return mock.Object;
    }

    private static IHolidayService DefaultHolidayService()
    {
        var mock = new Mock<IHolidayService>();
        mock.Setup(x => x.FetchHolidaysForMonth(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Holiday>());

        return mock.Object;
    }

    private static ITimeOffService DefaultTimeOffService()
    {
        var mock = new Mock<ITimeOffService>();
        mock.Setup(x => x.GetAllForTrainerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeOff>());

        return mock.Object;
    }

    private static ITrainingSessionsService DefaultTrainingSessionsService()
    {
        var mock = new Mock<ITrainingSessionsService>();
        mock.Setup(x => x.GetTrainingSessionsForTrainerIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TrainingSession>());

        return mock.Object;
    }

    private static CalendarService CreateCalendarService(
        ITrainerAvailabilitiesService? trainerAvailabilitiesService = null,
        IHolidayService? holidayService = null,
        ITimeOffService? timeOffService = null,
        ITrainingSessionsService? trainingSessionsService = null) =>
        new(
            trainerAvailabilitiesService ?? DefaultTrainerAvailabilitiesService(),
            holidayService ?? DefaultHolidayService(),
            timeOffService ?? DefaultTimeOffService(),
            trainingSessionsService ?? DefaultTrainingSessionsService());
}
