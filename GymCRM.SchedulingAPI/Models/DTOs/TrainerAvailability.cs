using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Models.DTOs;

public class TrainerAvailability
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public bool WorkingWeekends { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
    public List<TrainerDailyAvailability> DailyAvailabilities { get; set; }
}