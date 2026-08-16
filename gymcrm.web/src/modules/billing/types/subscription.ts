// Mirrors GymCRM.BillingAPI.Models.DTOs.Subscription 1:1.
export interface Subscription {
    id: string;
    memberAccountGuid: string;
    planType: number;
    status: number;
    startDate: string;
    nextRenewalDate?: string;
    dateCreated: string;
    dateModified: string;
}
