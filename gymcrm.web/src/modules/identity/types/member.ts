// Mirrors GymCRM.IdentityAPI.Models.DTOs.Member 1:1.
export interface Member {
    accountGuid?: string;
    accountType: number;
    firstName?: string;
    middleName?: string;
    lastName?: string;
    email: string;
    phoneNumber?: string;
    mobileNumber?: string;
    gender: number;
    personalTrainerId?: string;
    workoutGroupIds?: string[];
    workingExperienceInMonths?: number;
    gymSubscriptionType: number;
    timeZone: string;
    dateOfBirth?: string;
    hourlyPrice?: number;
    hasPhoto: boolean;
}
