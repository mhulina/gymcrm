// Mirrors GymCRM.SchedulingAPI.Models.DTOs.InsertTrainingSession 1:1.
// trainerId/clientId must be AccountGuids (see trainingSession.ts).
// startTime/endTime MUST be built as naive local datetime strings via
// utils/calendarDate.ts's buildLocalDateTime() - never Date.prototype.toISOString(),
// which would UTC-shift by the browser's offset. BookingValidationService and
// TrainerAvailabilitiesService.IsTrainerWorkingOnDateAsync compare wall-clock
// time directly with no timezone conversion, so a shifted timestamp would
// silently validate against - and book - the wrong hour.
export interface InsertTrainingSession {
    trainerId: string;
    clientId: string;
    startTime: string;
    endTime: string;
    description?: string;
}
