import {InsertDailyAvailability} from "./insertDailyAvailability";

// Mirrors GymCRM.SchedulingAPI.Models.DTOs.InsertAvailability 1:1.
// Body for POST /AddAvailability - one-shot creation of a trainer's whole week.
// Only valid when the trainer has no TrainerAvailability row yet; the backend
// has no uniqueness check, so calling this twice creates a duplicate row.
export interface InsertAvailability {
    trainerId: string;
    workingWeekends: boolean;
    dailyAvailabilities: InsertDailyAvailability[];
}
