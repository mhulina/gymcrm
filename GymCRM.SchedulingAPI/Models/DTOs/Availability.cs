namespace GymCRM.SchedulingAPI.Models.DTOs;

public class Availability
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public int DayOfWeek { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsAvailable { get; set; }
}