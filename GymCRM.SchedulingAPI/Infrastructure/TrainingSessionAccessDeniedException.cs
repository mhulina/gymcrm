namespace GymCRM.SchedulingAPI.Models;

public class TrainingSessionAccessDeniedException : Exception
{
    public TrainingSessionAccessDeniedException(string message = "You are not allowed to modify this training session") : base(message) { }
    public TrainingSessionAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
