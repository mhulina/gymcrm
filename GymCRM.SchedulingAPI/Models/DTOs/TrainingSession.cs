namespace GymCRM.SchedulingAPI.Models.DTOs;

public class TrainingSession
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Status { get; set; }
    public string? Description { get; set; }
}