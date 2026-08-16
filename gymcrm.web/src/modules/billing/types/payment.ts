// Mirrors GymCRM.BillingAPI.Models.DTOs.Payment 1:1.
export interface Payment {
    id: string;
    subscriptionId: string;
    amount: number;
    method: number;
    status: number;
    paidAt: string;
    externalReference?: string;
    dateCreated: string;
}
