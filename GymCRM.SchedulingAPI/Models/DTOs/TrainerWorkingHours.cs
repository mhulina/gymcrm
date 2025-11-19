namespace GymCRM.SchedulingAPI.Models.DTOs;

public class TrainerWorkingHours
{
    public Guid Id { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
    public TrainerDailyAvailability DailyAvailability { get; set; }
}