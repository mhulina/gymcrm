// Mirrors GymCRM.SchedulingAPI.Models.Enums.TrainingSessionStatus 1:1.
// Wired as a raw int on the wire, not a string enum.
export enum TrainingSessionStatus {
    Booked,
    Cancelled,
    Completed,
    NoShow,
    Reschedule,
}
