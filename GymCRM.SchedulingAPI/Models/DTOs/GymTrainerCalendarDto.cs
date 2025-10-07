namespace GymCRM.SchedulingAPI.Models.DTOs;

public class GymTrainerCalendarDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public Guid TrainerId { get; set; }
    public List<Availability> Availabilities { get; set; }
    public List<TimeOff> TimeOffs { get; set; }
    public List<TrainingSession> TrainingSessions { get; set; }
}