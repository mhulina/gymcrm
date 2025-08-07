namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertAvailability
{
    public Guid TrainerId { get; set; }
    public int DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAvailable { get; set; }
}