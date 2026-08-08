namespace GymCRM.SchedulingAPI.Models.DTOs;

public class InsertTimeOff
{
    public Guid TrainerId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; }
}