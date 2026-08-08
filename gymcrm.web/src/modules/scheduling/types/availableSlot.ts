// Mirrors GymCRM.SchedulingAPI.Models.DTOs.AvailableSlot 1:1.
// startTime is a naive trainer-local datetime string ("YYYY-MM-DDTHH:mm:ss"), same convention
// as everywhere else in this module - no timezone conversion happens on the wire.
export interface AvailableSlot {
    startTime: string;
    availableDurationsMinutes: number[];
}
