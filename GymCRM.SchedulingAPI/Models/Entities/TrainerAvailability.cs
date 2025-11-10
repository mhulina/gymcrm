namespace GymCRM.SchedulingAPI.Models.Entities;

public class TrainerAvailability
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public int DayOfWeek { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime DateModifiedUtc { get; set; }
}