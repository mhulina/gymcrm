namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertDailyAvailability
{
    public string DayOfWeek { get; set; }
    public bool IsDayOff { get; set; }
    public List<InsertWorkingHours> WorkingHours { get; set; }
}