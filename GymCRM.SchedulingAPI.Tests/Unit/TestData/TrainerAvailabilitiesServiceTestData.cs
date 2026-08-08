namespace GymCRM.SchedulingAPI.Tests.Unit.TestData;

public class TrainerAvailabilitiesServiceTestData
{
    public static TheoryData<DateTime, DateTime> InvalidStartOrEndTimesForAvailabilityCheck() => new()
    {
        { DateTime.MinValue, DateTime.UtcNow },
        { DateTime.MaxValue, DateTime.UtcNow },
        { DateTime.UtcNow, DateTime.MinValue },
        { DateTime.UtcNow, DateTime.MaxValue }
    };
}
