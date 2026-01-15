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

        var baseTrainerId = Guid.CreateVersion7();
        var baseClientId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(11);

        // Factory functions to create fresh instances for each test case
        var createHolidays = (DateTime baseDate) => new List<Holiday>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                CountryCode = "HR",
                Date = baseDate.AddDays(4),
                EnglishName = "TestoHoliday",
                LocalName = "TestoHoliday",
                RegionCode = string.Empty,
                Type = "Public",
                Year = baseDate.AddDays(4).Year
            }
        };

        var createTimeOffs = (DateTime baseDate, Guid trainerId) => new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = baseDate.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = trainerId
            }
        };

        var validationResult = ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting);

        // ========================================================================
        // SCENARIO 1: Back-to-back sessions (0-minute gap) - SHOULD FAIL
        // ========================================================================
        var booking1 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession1 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime.AddMinutes(-60),
                EndTime = baseStartTime,
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking1,
            true,
            createTimeOffs(baseStartTime, baseTrainerId),
            existingSession1,
            createHolidays(baseStartTime),
            validationResult);

        // ========================================================================
        // SCENARIO 2: 14-minute gap after existing session - SHOULD FAIL
        // ========================================================================
        var booking2 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession2 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime.AddMinutes(-74),
                EndTime = baseStartTime.AddMinutes(-14),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking2,
            true,
            createTimeOffs(baseStartTime, baseTrainerId),
            existingSession2,
            createHolidays(baseStartTime),
            validationResult);

        // ========================================================================
        // SCENARIO 3: Exact time overlap (same start time) - SHOULD FAIL
        // ========================================================================
        var booking3 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession3 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime,
                EndTime = baseStartTime.AddMinutes(60),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking3,
            true,
            createTimeOffs(baseStartTime, baseTrainerId),
            existingSession3,
            createHolidays(baseStartTime),
            validationResult);

        // ========================================================================
        // SCENARIO 4: 14-minute gap before existing session - SHOULD FAIL
        // ========================================================================
        var booking4 = new InsertTrainingSession
        {
            ClientId = baseClientId,
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession4 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime.AddMinutes(14),
                EndTime = baseStartTime.AddMinutes(74),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking4,
            true,
            createTimeOffs(baseStartTime, baseTrainerId),
            existingSession4,
            createHolidays(baseStartTime),
            validationResult);

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

        var baseTrainerId = Guid.CreateVersion7();
        var baseStartTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        
        var createHolidays = () => new List<Holiday>
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

        var createTimeOffs = () => new List<TimeOff>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                Date = baseStartTime.AddDays(3),
                Reason = "TestoTimeOff",
                TrainerId = baseTrainerId
            }
        };

        // ========================================================================
        // SCENARIO 1: Existing session completely inside new booking
        // ========================================================================
        var booking1 = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(90)
        };
        var existingSession1 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime.AddMinutes(20),
                EndTime = baseStartTime.AddMinutes(70),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking1,
            true,
            createTimeOffs(),
            existingSession1,
            createHolidays(),
            ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting));

        // ========================================================================
        // SCENARIO 2: New booking overlaps at END of existing session
        // ========================================================================
        var booking2 = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(90)
        };
        var existingSession2 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime,
                EndTime = baseStartTime.AddMinutes(60),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking2,
            true,
            createTimeOffs(),
            existingSession2,
            createHolidays(),
            ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting));

        // ========================================================================
        // SCENARIO 3: New booking overlaps at START of existing session
        // ========================================================================
        var booking3 = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = baseTrainerId,
            StartTime = baseStartTime,
            EndTime = baseStartTime.AddMinutes(90)
        };
        var existingSession3 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime.AddMinutes(30),
                EndTime = baseStartTime.AddMinutes(90),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking3,
            true,
            createTimeOffs(),
            existingSession3,
            createHolidays(),
            ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting));

        // ========================================================================
        // SCENARIO 4: New booking completely inside existing session
        // ========================================================================
        var booking4 = new InsertTrainingSession
        {
            ClientId = Guid.CreateVersion7(),
            TrainerId = baseTrainerId,
            StartTime = baseStartTime.AddMinutes(30),
            EndTime = baseStartTime.AddMinutes(60)
        };
        var existingSession4 = new List<TrainingSession>
        {
            new()
            {
                Id = Guid.CreateVersion7(),
                StartTime = baseStartTime,
                EndTime = baseStartTime.AddMinutes(90),
                TrainerId = baseTrainerId,
                ClientId = Guid.CreateVersion7(),
                Status = (int)TrainingSessionStatus.Booked
            }
        };
        theoryData.Add(
            booking4,
            true,
            createTimeOffs(),
            existingSession4,
            createHolidays(),
            ValidationResult.Fail(ValidationMessages.BookingOverlapsExisting));

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