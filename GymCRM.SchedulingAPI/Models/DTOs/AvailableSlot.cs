namespace GymCRM.SchedulingAPI.Models.DTOs;

public class AvailableSlot
{
    public DateTime StartTime { get; set; }
    public List<int> AvailableDurationsMinutes { get; set; } = new();
}
