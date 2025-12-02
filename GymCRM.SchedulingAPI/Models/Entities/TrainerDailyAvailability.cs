namespace GymCRM.SchedulingAPI.Models.Entities;

public class TrainerDailyAvailability
{
    public Guid Id { get; set; }
    public string DayOfWeek { get; set; }
    public bool IsDayOff { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
    public Guid AvailabilityId { get; set; }
}