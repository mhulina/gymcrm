// Mirrors GymCRM.BillingAPI.Models.DTOs.InsertPayment 1:1.
export interface InsertPayment {
    subscriptionId: string;
    amount: number;
    method: number;
    status: number;
    paidAt?: string;
    externalReference?: string;
}
