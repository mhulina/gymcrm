using FluentAssertions;
using GymCRM.SchedulingAPI.Constants;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Enums;
using GymCRM.SchedulingAPI.Services.Implementation;
using GymCRM.SchedulingAPI.Services.Interface;
using GymCRM.SchedulingAPI.Tests.Unit.TestData;
using Moq;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestBookingValidationService
{
    [Fact]
    public async Task GivenInvalidBookingParameter_WhenValidatingBooking_ThenExceptionIsThrown()
    {
        // Given
        var service = CreateBookingValidationService();
        
        // When
        Func<Task> act = async () => await service.ValidateBookingAsync(null);
        
        // Then
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [MemberData(
        nameof(BookingValidationServiceTestData.InvalidDatesForBooking), 
        MemberType = typeof(BookingValidationServiceTestData))]
    public async Task GivenInvalidBookingParameter_WhenValidatingBooking_ThenFailedResultIsReturned(
        DateTime startDate, 
        DateTime endDate)
    {
        // Given
        var service = CreateBookingValidationService();
        
        // When
        var result = await service.ValidateBookingAsync(new InsertTrainingSession
        {
            StartTime = startDate,
            EndTime = endDate
        });
        
        // Then
        result.Should().NotBeNull();
        result.Should().BeOfType<ValidationResult>();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1).And.Contain(ValidationMessages.BookingInPast);
    }

    [Theory]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingStartAndEndAreEqual), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingEndDateBeforeStartDate), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingIsOnAHoliday), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingIsOnTrainerTimeOff), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingTooShort), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingTooLong), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingInvalidInterval), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.TrainerIsNotWorkingOnBookingDate), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingOverlapsWithExistingTrainingSession), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.BookingStartsWithinBufferTimeOfExistingTrainingSession), 
        MemberType = typeof(BookingValidationServiceTestData))]
    [MemberData(
        nameof(BookingValidationServiceTestData.ValidBooking), 
        MemberType = typeof(BookingValidationServiceTestData))]
    public async Task GivenValidBookingParameter_WhenValidatingBooking_ThenExpectedResultIsReturned(
        InsertTrainingSession trainingSession,
        bool isTrainerWorkingOnBookingDate,
        List<TimeOff> trainerTimeOffsInMonth,
        List<TrainingSession> trainerTrainingSessionsInMonth,
        List<Holiday> holidaysInMonth,
        ValidationResult expectedResult)
    {
        // Given
        var trainerAvailabilitiesServiceMock = new Mock<ITrainerAvailabilitiesService>();
        trainerAvailabilitiesServiceMock
            .Setup(x => x.IsTrainerWorkingOnDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(isTrainerWorkingOnBookingDate);
        var timeOffServiceMock = new Mock<ITimeOffService>();
        timeOffServiceMock
            .Setup(x => x.GetAllForTrainerIdInMonthAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainerTimeOffsInMonth);
        var trainingSessionsServiceMock =  new Mock<ITrainingSessionsService>();
        trainingSessionsServiceMock
            .Setup(x => x.GetTrainingSessionsForTrainerIdInMonthAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainerTrainingSessionsInMonth);
        var holidayServiceMock = new Mock<IHolidayService>();
        holidayServiceMock
            .Setup(x => x.FetchHolidaysForMonth(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(holidaysInMonth);
        var service = CreateBookingValidationService(
            trainerAvailabilitiesService: trainerAvailabilitiesServiceMock.Object,
            trainingSessionsService: trainingSessionsServiceMock.Object,
            timeOffService: timeOffServiceMock.Object,
            holidayService: holidayServiceMock.Object);
        
        // When
        var result = await service.ValidateBookingAsync(trainingSession);
        
        // Then
        result.Should().NotBeNull();
        result.IsValid.Should().Be(expectedResult.IsValid);
        result.Errors.Should().HaveCount(expectedResult.Errors.Count);
        result.Errors.Should().BeEquivalentTo(expectedResult.Errors);
    }

    [Fact]
    public async Task GivenOverlappingSessionMatchesExcludeSessionId_WhenValidatingBooking_ThenValidationSucceeds()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var booking = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = trainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var overlappingSession = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = trainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        var service = CreateBookingValidationServiceForOverlapScenario(new List<TrainingSession> { overlappingSession });

        // When
        var result = await service.ValidateBookingAsync(booking, excludeSessionId: overlappingSession.Id);

        // Then
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenOverlappingSessionWithDifferentId_WhenValidatingBookingWithExcludeSessionId_ThenValidationStillFails()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var booking = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = trainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var overlappingSession = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = trainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        var service = CreateBookingValidationServiceForOverlapScenario(new List<TrainingSession> { overlappingSession });

        // When
        var result = await service.ValidateBookingAsync(booking, excludeSessionId: Guid.CreateVersion7());

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(ValidationMessages.BookingOverlapsExisting);
    }

    [Theory]
    [InlineData(TrainingSessionStatus.Cancelled)]
    [InlineData(TrainingSessionStatus.Completed)]
    [InlineData(TrainingSessionStatus.NoShow)]
    [InlineData(TrainingSessionStatus.Reschedule)]
    public async Task GivenOverlappingSessionWithInactiveStatus_WhenValidatingBooking_ThenValidationSucceeds(
        TrainingSessionStatus inactiveStatus)
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var booking = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = trainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var overlappingSession = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = trainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)inactiveStatus
        };
        var service = CreateBookingValidationServiceForOverlapScenario(new List<TrainingSession> { overlappingSession });

        // When
        var result = await service.ValidateBookingAsync(booking);

        // Then
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GivenOverlappingRequestedSession_WhenValidatingBooking_ThenValidationFails()
    {
        // Given
        var trainerId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var booking = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = trainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var overlappingSession = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = trainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Requested
        };
        var service = CreateBookingValidationServiceForOverlapScenario(new List<TrainingSession> { overlappingSession });

        // When
        var result = await service.ValidateBookingAsync(booking);

        // Then
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(ValidationMessages.BookingOverlapsExisting);
    }

    private BookingValidationService CreateBookingValidationService(
        ITrainerAvailabilitiesService trainerAvailabilitiesService = null,
        ITrainingSessionsService trainingSessionsService = null,
        ITimeOffService timeOffService = null,
        IHolidayService holidayService = null)
    {
        var service = new BookingValidationService(
            trainerAvailabilitiesService,
            trainingSessionsService,
            timeOffService,
            holidayService);

        return service;
    }

    // Builds a service where the trainer is working, has no time off/holidays, and the only
    // thing that can fail validation is the overlap check against the given sessions - isolates
    // the excludeSessionId/active-status filtering behaviour from the rest of the pipeline.
    private BookingValidationService CreateBookingValidationServiceForOverlapScenario(
        List<TrainingSession> trainerSessionsInMonth)
    {
        var trainerAvailabilitiesServiceMock = new Mock<ITrainerAvailabilitiesService>();
        trainerAvailabilitiesServiceMock
            .Setup(x => x.IsTrainerWorkingOnDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var timeOffServiceMock = new Mock<ITimeOffService>();
        timeOffServiceMock
            .Setup(x => x.GetAllForTrainerIdInMonthAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeOff>());
        var trainingSessionsServiceMock = new Mock<ITrainingSessionsService>();
        trainingSessionsServiceMock
            .Setup(x => x.GetTrainingSessionsForTrainerIdInMonthAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(trainerSessionsInMonth);
        var holidayServiceMock = new Mock<IHolidayService>();
        holidayServiceMock
            .Setup(x => x.FetchHolidaysForMonth(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Holiday>());

        return CreateBookingValidationService(
            trainerAvailabilitiesService: trainerAvailabilitiesServiceMock.Object,
            trainingSessionsService: trainingSessionsServiceMock.Object,
            timeOffService: timeOffServiceMock.Object,
            holidayService: holidayServiceMock.Object);
    }
}