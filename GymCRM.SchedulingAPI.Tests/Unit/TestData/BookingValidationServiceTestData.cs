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
        ValidationResult> BookingStartsWithinBufferTimeOfExistingTrainingSession()
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
        var trainingSession = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = insertTrainingSession.StartTime.AddMinutes(-60),
            EndTime = insertTrainingSession.EndTime.AddMinutes(-60),
            TrainerId = insertTrainingSession.TrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        
        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting);
        theoryData.Add(insertTrainingSession, true, timeOffs, [trainingSession], holidays, validationResult);

        trainingSession.StartTime = trainingSession.StartTime.AddMinutes(-14);
        trainingSession.EndTime = trainingSession.EndTime.AddMinutes(-14);
        theoryData.Add(insertTrainingSession, true, timeOffs, [trainingSession], holidays, validationResult);

        trainingSession.StartTime = trainingSession.StartTime.AddMinutes(74);
        trainingSession.EndTime = trainingSession.EndTime.AddMinutes(74);
        theoryData.Add(insertTrainingSession, true, timeOffs, [trainingSession], holidays, validationResult);

        trainingSession.StartTime = trainingSession.StartTime.AddMinutes(14);
        trainingSession.EndTime = trainingSession.EndTime.AddMinutes(14);
        theoryData.Add(insertTrainingSession, true, timeOffs, [trainingSession], holidays, validationResult);
        
        return theoryData;
    }
    
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

        var baseTrainerId = Guid.CreateVersion7();
        var baseClientId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var holidays = new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = baseStartTime.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = baseStartTime.AddDays(4).Year
            }
        };
        var timeOffs = new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = baseStartTime.Date.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = baseTrainerId
            }
        };
        
        // ========================================================================
        // SCENARIO 1: Valid 30-minute booking with existing session 1 day later
        // ========================================================================
        var booking1 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(30)
        };
        var existingSession1 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime.AddDays(1),
            EndTime = baseStartTime.AddDays(1).AddMinutes(30),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking1,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession1 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

        // ========================================================================
        // SCENARIO 2: Valid 60-minute booking with existing session 1 day later
        // ========================================================================
        var booking2 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession2 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime.AddDays(1),
            EndTime = baseStartTime.AddDays(1).AddMinutes(30),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking2,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession2 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

        // ========================================================================
        // SCENARIO 3: Valid 90-minute booking with existing session 1 day later
        // ========================================================================
        var booking3 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(90)
        };
        var existingSession3 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime.AddDays(1),
            EndTime = baseStartTime.AddDays(1).AddMinutes(30),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking3,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession3 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

        // ========================================================================
        // SCENARIO 4: Valid booking - exactly 15 minutes AFTER existing session
        // ========================================================================
        var booking4 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime.AddMinutes(75),
            EndTime = baseStartTime.AddMinutes(135)
        };
        var existingSession4 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking4,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession4 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

        // ========================================================================
        // SCENARIO 5: Valid booking - exactly 15 minutes BEFORE existing session
        // ========================================================================
        var booking5 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession5 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime.AddMinutes(75),
            EndTime = baseStartTime.AddMinutes(135),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking5,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession5 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

        // ========================================================================
        // SCENARIO 6: Valid booking - 16 minutes AFTER existing session
        // ========================================================================
        var booking6 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime.AddMinutes(76),
            EndTime = baseStartTime.AddMinutes(136)
        };
        var existingSession6 = new TrainingSession
        {
            Id = Guid.CreateVersion7(),
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60),
            TrainerId = baseTrainerId,
            ClientId = Guid.CreateVersion7(),
            Status = (int)TrainingSessionStatus.Booked
        };
        theoryData.Add(
            booking6,
            true,
            new List<TimeOff>(timeOffs),
            new List<TrainingSession> { existingSession6 },
            new List<Holiday>(holidays),
            ValidationResult.Success());

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