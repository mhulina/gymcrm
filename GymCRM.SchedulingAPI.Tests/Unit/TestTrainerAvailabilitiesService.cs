using GymCRM.SchedulingAPI.Models.Entities;

namespace GymCRM.SchedulingAPI.Tests.Unit;

public class TestTrainerAvailabilitiesService
{
    private List<TrainerAvailability> CreateTrainerAvailability()
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

    private List<TrainerDailyAvailability> CreateTrainerDailyAvailability(
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

    private List<TrainerWorkingHours> CreateTrainerWorkingHours(
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
}