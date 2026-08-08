// Mirrors GymCRM.SchedulingAPI.Models.DTOs.TrainerWorkingHours 1:1.
// StartTime/EndTime wire as "HH:mm" strings (see GymCRM.Shared/Utilities/TimeOnlyJsonConverter.cs),
// which is exactly what <input type="time"> produces and accepts.
export interface TrainerWorkingHours {
    id: string;
    startTime: string;
    endTime: string;
    dateCreatedUtc: string;
    dateModifiedUtc: string;
}
