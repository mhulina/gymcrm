namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertAvailability
{
    public Guid TrainerId { get; set; }
    public bool WorkingWeekends { get; set; }
    public List<InsertDailyAvailability> DailyAvailabilities { get; set; }
}