import {TrainerWorkingHours} from "./trainerWorkingHours";

// Mirrors GymCRM.SchedulingAPI.Models.DTOs.TrainerDailyAvailability 1:1.
// dayOfWeek must be one of System.DayOfWeek's names (see constants/daysOfWeek.ts).
export interface TrainerDailyAvailability {
    id: string;
    dayOfWeek: string;
    isDayOff: boolean;
    dateCreatedUtc: string;
    dateModifiedUtc: string;
    workingHours: TrainerWorkingHours[];
}
