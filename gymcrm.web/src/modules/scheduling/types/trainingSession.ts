// Mirrors GymCRM.SchedulingAPI.Models.DTOs.TrainingSession 1:1.
// trainerId/clientId are the trainer's/member's AccountGuid, not Member.Id
// (Member has no Id field - only accountGuid - see types/member.ts).
// startTime/endTime are naive local datetime strings ("YYYY-MM-DDTHH:mm:ss") -
// no timezone conversion happens anywhere in this module.
export interface TrainingSession {
    id: string;
    trainerId: string;
    clientId: string;
    startTime: string;
    endTime: string;
    status: number;
    description?: string;
}
