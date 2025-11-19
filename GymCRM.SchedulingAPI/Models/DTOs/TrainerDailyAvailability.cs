using GymCRM.SchedulingAPI.Models.DTOs;

namespace GymCRM.SchedulingAPI.Models.DTOs;

public class TrainerDailyAvailability
{
    public Guid Id { get; set; }
    public string DayOfWeek { get; set; }
    public bool IsDayOff { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
    public List<TrainerWorkingHours> WorkingHours { get; set; }
}