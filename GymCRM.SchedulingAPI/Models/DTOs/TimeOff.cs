namespace GymCRM.SchedulingAPI.Models.DTOs;

public class TimeOff
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; }
}