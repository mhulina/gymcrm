namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertWorkingHours
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
}