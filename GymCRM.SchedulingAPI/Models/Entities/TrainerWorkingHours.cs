namespace GymCRM.SchedulingAPI.Models.Entities;

public class TrainerWorkingHours
{
    public Guid Id { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
    public Guid DailyAvailabilityId { get; set; }
}