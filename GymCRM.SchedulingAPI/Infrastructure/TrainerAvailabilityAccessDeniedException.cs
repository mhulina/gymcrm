namespace GymCRM.SchedulingAPI.Models;

public class TrainerAvailabilityAccessDeniedException : Exception
{
    public TrainerAvailabilityAccessDeniedException(string message = "You are not allowed to modify this trainer's availability") : base(message) { }
    public TrainerAvailabilityAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
