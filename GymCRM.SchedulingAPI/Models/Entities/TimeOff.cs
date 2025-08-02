namespace GymCRM.SchedulingAPI.Models.Entities;

public class TimeOff
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public DateOnly Date { get; set; }
    public string Reason { get; set; }
}