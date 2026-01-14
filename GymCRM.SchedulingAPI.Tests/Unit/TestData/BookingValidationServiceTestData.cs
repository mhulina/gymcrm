using GymCRM.SchedulingAPI.Constants;
using GymCRM.SchedulingAPI.Models.DTOs;
using GymCRM.SchedulingAPI.Models.Enums;

namespace GymCRM.SchedulingAPI.Tests.Unit.TestData;

public class BookingValidationServiceTestData
{
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingEndDateBeforeStartDate()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        var trainingSessions = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = insertTrainingSession.StartTime.AddDays(1),
                EndTime = insertTrainingSession.EndTime.AddDays(1),
                TrainerId = insertTrainingSession.TrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingInPast);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingStartAndEndAreEqual()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        var trainingSessions = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = insertTrainingSession.StartTime.AddDays(1),
                EndTime = insertTrainingSession.EndTime.AddDays(1),
                TrainerId = insertTrainingSession.TrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingTooShort);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> ValidBooking()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        var trainingSessions = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = insertTrainingSession.StartTime.AddDays(1),
                EndTime = insertTrainingSession.EndTime.AddDays(1),
                TrainerId = insertTrainingSession.TrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        
        var validationResult = ValidationResult.Success();
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(30);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(30);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingOverlapsWithExistingTrainingSession()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(90)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        var trainingSessions = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = insertTrainingSession.StartTime.AddMinutes(20),
                EndTime = insertTrainingSession.EndTime.AddMinutes(-10),
                TrainerId = insertTrainingSession.TrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);
        
        trainingSessions
            .FirstOrDefault().EndTime = insertTrainingSession.EndTime.AddMinutes(-30);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);
        
        trainingSessions
            .FirstOrDefault().EndTime = insertTrainingSession.EndTime;
        insertTrainingSession.StartTime = insertTrainingSession.StartTime.AddMinutes(30);
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(-30);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);

        insertTrainingSession.StartTime = insertTrainingSession.StartTime.AddMinutes(-30);
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(30);
        trainingSessions
            .FirstOrDefault().StartTime = insertTrainingSession.StartTime.AddMinutes(-20);
        trainingSessions
            .FirstOrDefault().EndTime = insertTrainingSession.EndTime.AddMinutes(-20);
        theoryData.Add(insertTrainingSession, true, timeOffs, trainingSessions, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> TrainerIsNotWorkingOnBookingDate()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(60)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOutsideAvailability);
        theoryData.Add(insertTrainingSession, false, timeOffs, null, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingInvalidInterval()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(67)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingIntervals);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(22);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(-30);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(-28);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingTooLong()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(95)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingTooLong);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(-4);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingTooShort()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(25)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingTooShort);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);
        
        insertTrainingSession.EndTime = insertTrainingSession.EndTime.AddMinutes(4);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);

        return theoryData;
    }
    
    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingIsOnTrainerTimeOff()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(60)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = insertTrainingSession.StartTime.Date,
                Reason = "TestoTimeOff",
                TrainerId = insertTrainingSession.TrainerId
            }
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOutsideAvailability);
        theoryData.Add(insertTrainingSession, true, timeOffs, null, holidays, validationResult);

        return theoryData;
    }

    public static TheoryData<
        InsertTrainingSession,
        bool,
        List<TimeOff>,
        List<TrainingSession>,
        List<Holiday>,
        ValidationResult> BookingIsOnAHoliday()
    {
        var theoryData = new TheoryData<
            InsertTrainingSession,
            bool,
            List<TimeOff>,
            List<TrainingSession>,
            List<Holiday>,
            ValidationResult>();

        var insertTrainingSession = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = Guid.CreateVersion7(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(60)
        };
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = insertTrainingSession.StartTime.Date,
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = insertTrainingSession.StartTime.Year
            }
        };
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOnHoliday);
        theoryData.Add(insertTrainingSession, true, null, null, holidays, validationResult);

        return theoryData;
    }

    public static TheoryData<DateTime, DateTime> InvalidDatesForBooking()
    {
        var theoryData = new TheoryData<DateTime, DateTime>
        {
            {
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)
            },
            {
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(-1)
            }
        };

        return theoryData;
    }
}