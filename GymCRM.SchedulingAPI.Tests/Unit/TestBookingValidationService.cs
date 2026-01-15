using FluentAssertions;
using GymCRM.SchedulingAPI.Constants;
using GymCRM.SchedulingAPI.Models.DTOs;
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
}