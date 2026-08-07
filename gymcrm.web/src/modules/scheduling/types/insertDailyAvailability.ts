import {InsertWorkingHours} from "./insertWorkingHours";

// Mirrors GymCRM.SchedulingAPI.Models.DTOs.InsertDailyAvailability 1:1.
export interface InsertDailyAvailability {
    dayOfWeek: string;
    isDayOff: boolean;
    workingHours: InsertWorkingHours[];
}
