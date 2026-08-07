import {TrainerDailyAvailability} from "./trainerDailyAvailability";

// Mirrors GymCRM.SchedulingAPI.Models.DTOs.TrainerAvailability 1:1.
// Note: PUT UpdateAvailability only persists the top-level fields below (id,
// trainerId, workingWeekends) - the entity has no nested DailyAvailabilities
// collection, so AutoMapper silently drops dailyAvailabilities on update. It's
// only populated on reads (GetAvailabilitiesForTrainerId) and on first creation
// (POST AddAvailability, via InsertAvailability).
export interface TrainerAvailability {
    id: string;
    trainerId: string;
    workingWeekends: boolean;
    dateCreatedUtc: string;
    dateModifiedUtc: string;
    dailyAvailabilities: TrainerDailyAvailability[];
}
