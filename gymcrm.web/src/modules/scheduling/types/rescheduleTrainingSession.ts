// Mirrors GymCRM.SchedulingAPI.Models.DTOs.RescheduleTrainingSession 1:1.
// newStartTime/newEndTime MUST be built via utils/calendarDate.ts's buildLocalDateTime()
// (see insertTrainingSession.ts) - naive trainer-local datetimes, no timezone conversion
// needed since a trainer reschedules their own session in their own timezone.
export interface RescheduleTrainingSession {
    newStartTime: string;
    newEndTime: string;
}
