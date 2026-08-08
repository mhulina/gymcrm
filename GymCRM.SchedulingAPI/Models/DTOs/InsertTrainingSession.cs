namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertTrainingSession
{
    public Guid TrainerId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
}