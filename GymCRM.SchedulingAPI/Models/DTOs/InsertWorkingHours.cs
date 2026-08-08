namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertWorkingHours
{
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}