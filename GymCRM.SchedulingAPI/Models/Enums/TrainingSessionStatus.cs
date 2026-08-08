namespace GymCRM.SchedulingAPI.Models.Enums;

public enum TrainingSessionStatus
{
    Booked,
    Cancelled,
    Completed,
    NoShow,
    Reschedule,
    // Appended, not inserted - Status is a plain persisted int, never renumber existing values.
    Requested
}