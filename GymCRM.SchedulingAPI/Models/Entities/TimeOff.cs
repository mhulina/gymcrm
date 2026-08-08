namespace GymCRM.SchedulingAPI.Models.Entities;

public class TimeOff
{
    public Guid Id { get; set; }
    public Guid TrainerId { get; set; }
    public DateTime Date { get; set; }
    public string Reason { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime DateModified { get; set; }
}