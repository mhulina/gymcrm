namespace GymCRM.SchedulingAPI.Models.Entities;

public class SessionType
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int DurationMinutes { get; set; }
}